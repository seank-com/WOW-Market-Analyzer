using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;

namespace WMA
{
    public class Database : DataContext
    {
        public Table<Configuration> Configurations;
        public Table<Realm> Realms;
        public Table<Auction> Auctions;

        private Database(string connection) : base(connection) { }

        public static Database Open(string dbName)
        {
            bool fRebuild = false;

            // In the event that we call DeleteDatabase, we need to make sure it gets closed.
            using (Database db = new Database(dbName))
            {
                if (!db.DatabaseExists())
                    fRebuild = true;

                if (!fRebuild)
                {
                    try
                    {
                        if (db.Configurations.Count() < 1 || db.Configurations.First().Version != Configuration.CurrentVersion)
                            fRebuild = true;
                    }
                    catch
                    {
                        fRebuild = true;
                    }

                    if (fRebuild)
                    {
                        db.DeleteDatabase();
                    }
                }
            }

            Database result = new Database(dbName);
            if (fRebuild)
            {
                result.CreateDatabase();

                Configuration cfg = new Configuration() { Version = Configuration.CurrentVersion };
                result.Configurations.InsertOnSubmit(cfg);
                result.SubmitChanges();
            }
            return result;
        }
    }

    [Table(Name = "Configuration")]
    public class Configuration
    {
        public const int CurrentVersion = 2;

        [Column(IsPrimaryKey = true, CanBeNull = false)]
        public int Version { get; set; }

        [Column]
        public string SelectedRealm { get; set; }

        [Column]
        public long lastUpdate { get; set; }
    }

    public enum RealmType
    {
        PvP,
        PvE,
        RP,
        RPPvP
    }

    public enum RealmPopulation
    {
        High,
        Medium,
        Low
    }

    [Table(Name = "Realm")]
    public class Realm
    {
        [Column(IsPrimaryKey = true, CanBeNull = false)]
        public string Name { get; set; }

        [Column(CanBeNull = false)]
        public string Slug { get; set; }

        [Column(CanBeNull = false)]
        public bool Queue { get; set; }

        [Column(CanBeNull = false)]
        public bool Status { get; set; }

        [Column(CanBeNull = false)]
        public RealmType Type { get; set; }

        [Column(CanBeNull = false)]
        public RealmPopulation Population { get; set; }

        public void Update(Realm srcRealm)
        {
            Name = srcRealm.Name;
            Slug = srcRealm.Slug;
            Queue = srcRealm.Queue;
            Status = srcRealm.Status;
            Population = srcRealm.Population;
            Type = srcRealm.Type;
        }
    }

    public enum AuctionTimeLeft
    {
        VeryLong,
        Long,
        Medium,
        Short
    }

    public enum AuctionFaction
    {
        Alliance,
        Horde,
        Neutral
    }

    public enum AuctionStatus
    {
        Active,
        Expired,
        Sold
    }
    
    [Table(Name = "Auction")]
    public class Auction
    {
        [Column(IsPrimaryKey = true, CanBeNull = false)]
        public long AuctionId { get; set; }

        [Column(CanBeNull = false)]
        public long ItemId { get; set; }

        [Column(CanBeNull = false)]
        public string Owner { get; set; }

        [Column(CanBeNull = false)]
        public long Bid { get; set; }

        public long BidGold { get { return Bid / 10000; } }

        public long BidSilver { get { return (Bid / 100) % 100; } }

        public long BidCopper { get { return Bid % 100; } }

        [Column(CanBeNull = false)]
        public long Buyout { get; set; }

        public long BuyoutGold { get { return Buyout / 10000; } }

        public long BuyoutSilver { get { return (Buyout / 100) % 100; } }

        public long BuyoutCopper { get { return Buyout % 100; } }
        
        [Column(CanBeNull = false)]
        public long Quantity { get; set; }

        [Column(CanBeNull = false)]
        public AuctionTimeLeft TimeLeft { get; set; }

        [Column(CanBeNull = false)]
        public AuctionFaction Faction { get; set; }

        [Column(CanBeNull = false)]
        public AuctionStatus Status { get; set; }

        [Column(CanBeNull = false)]
        public DateTime FirstSeen { get; set; }

        [Column(CanBeNull = false)]
        public DateTime LastSeen { get; set; }
    }

}
