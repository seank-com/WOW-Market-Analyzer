using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Data.Linq;

namespace WMA
{
    interface IBlizzardProgress
    {
        void UpdateProgress(int value, int max);
    }

    class BlizzardAPI
    {
        // Downloads the contents of the specified url as a string
        private static string GetWebString(string url)
        {
            string result;
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                webRequest.AllowAutoRedirect = true;
                WebResponse webResponse = webRequest.GetResponse();

                StreamReader reader = new StreamReader(webResponse.GetResponseStream());

                result = reader.ReadToEnd();

                reader.Close();
                webResponse.Close();
            }
            catch
            {
                result = "";
            }
            return result;
        }

        public static void UpdateRealms(Database db)
        {
            JsonValue status = JSON.JsonDecode(GetWebString("http://us.battle.net/api/wow/realm/status"));
            if (status.Object.ContainsKey("realms"))
            {
                var query = from realm in status.Object["realms"].Array
                                 select new Realm()
                                 {
                                     Name = realm.Object["name"].String,
                                     Slug = realm.Object["slug"].String,
                                     Queue = realm.Object["queue"].Boolean,
                                     Status = realm.Object["status"].Boolean,
                                     Population = realm.Object["population"].String == "high" ? RealmPopulation.High :
                                                     realm.Object["population"].String == "medium" ? RealmPopulation.Medium :
                                                     RealmPopulation.Low,
                                     Type = realm.Object["type"].String == "pve" ? RealmType.PvE :
                                             realm.Object["type"].String == "pvp" ? RealmType.PvP :
                                             realm.Object["type"].String == "rp" ? RealmType.RP :
                                             RealmType.RPPvP
                                 };

                foreach (Realm webRealm in query)
                {
                    Realm dbRealm = db.Realms.SingleOrDefault(realm => realm.Name == webRealm.Name);
                    if (dbRealm != null)
                    {
                        dbRealm.Update(webRealm);
                    }
                    else
                    {
                        db.Realms.InsertOnSubmit(webRealm);
                    }
                }
                db.SubmitChanges();

                // For each realm in the database see if it is still availabe from the web
                foreach (Realm dbRealm in db.Realms)
                {
                    Realm webRealm = query.SingleOrDefault(realm => realm.Name == dbRealm.Name);
                    if (webRealm == null)
                    {
                        db.Realms.DeleteOnSubmit(dbRealm);
                    }
                }
                db.SubmitChanges();

                if (db.Configurations.First().SelectedRealm != null)
                {
                    string selectedRealmName = db.Configurations.First().SelectedRealm;
                    Realm selectedRealm = db.Realms.SingleOrDefault(realm => realm.Name == selectedRealmName);
                    if (selectedRealm == null)
                    {
                        db.Configurations.First().SelectedRealm = null;
                        db.SubmitChanges();
                    }
                }
            }
        }

        public static void UpdateAuctions(Database db, string realmName, IBlizzardProgress bp)
        {
            Configuration cfg = db.Configurations.First();
            if (db.Configurations.First().SelectedRealm != realmName)
            {
                cfg.SelectedRealm = realmName;
                cfg.lastUpdate = 0;
                db.Auctions.DeleteAllOnSubmit(db.Auctions);
                db.SubmitChanges();
            }

            string realmSlug = db.Realms.Single(realm => realm.Name == realmName).Slug;
            string url = "http://us.battle.net/api/wow/auction/data/" + realmSlug;
            JsonValue status = JSON.JsonDecode(GetWebString(url));

            if (status.Object.ContainsKey("files"))
            {
                long lastModified = status.Object["files"].Array[0].Object["lastModified"].Long;
                if (cfg.lastUpdate != lastModified)
                {
                    cfg.lastUpdate = lastModified;
                    db.SubmitChanges();

                    url = status.Object["files"].Array[0].Object["url"].String;
                    status = JSON.JsonDecode(GetWebString(url));

                    Dictionary<long, bool> webIds = new Dictionary<long, bool>();
                    UpdateAuctions(db, status, AuctionFaction.Alliance, webIds, bp);
                    UpdateAuctions(db, status, AuctionFaction.Horde, webIds, bp);
                    UpdateAuctions(db, status, AuctionFaction.Neutral, webIds, bp);
                    CloseAuctions(db, webIds, bp);
                }
            }
        }

        private static void UpdateAuctions(Database db, JsonValue status, AuctionFaction faction, Dictionary<long, bool> webIds, IBlizzardProgress bp)
        {
            string factionName = (faction == AuctionFaction.Alliance) ? "alliance" :
                                 (faction == AuctionFaction.Horde) ? "horde" : 
                                 "neutral";

            if (status.Object.ContainsKey(factionName))
            {
                var query = from auction in status.Object[factionName].Object["auctions"].Array
                            select new Auction()
                            {
                                AuctionId = auction.Object["auc"].Long,
                                ItemId = auction.Object["item"].Long,
                                Owner = auction.Object["owner"].String,
                                Bid = auction.Object["bid"].Long,
                                Buyout = auction.Object["buyout"].Long,
                                Quantity = auction.Object["quantity"].Long,
                                TimeLeft = (auction.Object["timeLeft"].String == "VERY_LONG") ? AuctionTimeLeft.VeryLong :
                                           (auction.Object["timeLeft"].String == "LONG") ? AuctionTimeLeft.Long :
                                           (auction.Object["timeLeft"].String == "MEDIUM") ? AuctionTimeLeft.Medium :
                                        AuctionTimeLeft.Short,
                                Faction = faction,
                                Status = AuctionStatus.Active,
                                FirstSeen = DateTime.Now,
                                LastSeen = DateTime.Now
                            };

                int progressCurrent = 0;
                int progressCount = query.Count();
                bp.UpdateProgress(progressCurrent, progressCount);

                foreach (Auction webAuction in query)
                {
                    Auction dbAuction = db.Auctions.SingleOrDefault(auction => auction.AuctionId == webAuction.AuctionId);
                    if (dbAuction != null)
                    {
                        dbAuction.LastSeen = DateTime.Now;
                        dbAuction.TimeLeft = webAuction.TimeLeft;
                    }
                    else
                    {
                        db.Auctions.InsertOnSubmit(webAuction);
                    }

                    webIds[webAuction.AuctionId] = true;

                    progressCurrent++;
                    if (progressCurrent % 1000 == 0)
                    {
                        bp.UpdateProgress(progressCurrent, progressCount);
                        db.SubmitChanges();
                    }
                }

                bp.UpdateProgress(progressCount, progressCount);
                db.SubmitChanges();
            }
        }

        private static void CloseAuctions(Database db, Dictionary<long, bool> webIds, IBlizzardProgress bp)
        {
            var activeAuctions = (from auction in db.Auctions where auction.Status == AuctionStatus.Active select auction).ToList();

            int progressCurrent = 0;
            int progressCount = activeAuctions.Count;
            bp.UpdateProgress(progressCurrent, progressCount);

            foreach (Auction dbAuction in activeAuctions)
            {
                if (!webIds.ContainsKey(dbAuction.AuctionId))
                {
                    dbAuction.Status = GetAuctionStatus(dbAuction.TimeLeft, dbAuction.LastSeen);
                }

                progressCurrent++;
                if (progressCurrent % 1000 == 0)
                {
                    bp.UpdateProgress(progressCurrent, progressCount);
                    db.SubmitChanges();
                }
            }
            db.SubmitChanges();
        }

        private static AuctionStatus GetAuctionStatus(AuctionTimeLeft timeLeft, DateTime lastSeen)
        {
            DateTime dtNow = DateTime.Now;
            if (timeLeft == AuctionTimeLeft.VeryLong)
            {
                if (dtNow < lastSeen.AddHours(48)) 
                    return AuctionStatus.Sold;
            }
            else if (timeLeft == AuctionTimeLeft.Long)
            {
                if (dtNow < lastSeen.AddHours(12)) 
                    return AuctionStatus.Sold;
            }
            else if (timeLeft == AuctionTimeLeft.Medium)
            {
                if (dtNow < lastSeen.AddHours(2)) 
                    return AuctionStatus.Sold;
            }
            else if (timeLeft == AuctionTimeLeft.Short)
            {
                if (dtNow < lastSeen.AddMinutes(30)) 
                    return AuctionStatus.Sold;
            }
            return AuctionStatus.Expired;
        }
    }
}
