WOW-Market-Analyzer
===================

A program I wrote a while back to make use of [Blizzard's API](http://blizzard.github.com/api-wow-docs/) and play with Lynq and WPF.

I also wanted to explore synchronization of live data to create a historical view. Blizzard provides live data about the auctions currently listed on a given server. I was interested in seeing if I could devise a strategy to poll this data on a regular basis, logging when new auctions appeared and when old auction disappeared. Since auctions expire after 12/24/48 hours and Blizzard reports the time left on an auction as VeryLong/Long/Medium/Short I should be able to detect if an auction either bought out, sold to the highest bidder or expired. 

I eventually want to be able to display graphs for individual items and their successful sale prices over time.