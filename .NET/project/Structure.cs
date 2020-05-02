using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MarketLab {

    // Class for error
    public class RootObjectError {
        public bool success { get; set; }
        public string message { get; set; }
        public object resuts { get; set; }
    }

    // Class for information about market
    public class ResultInformationMarket {
        public string exchange { get; set; }
        public string market { get; set; }
        public DateTime first_record { get; set; }
        public DateTime last_record { get; set; }
        public int total_size { get; set; }
    }

    public class RootObjectInformationMarket {
        public bool success { get; set; }
        public string message { get; set; }
        public ResultInformationMarket results { get; set; }
    }

    // Class for list of exchanges
    public class ResultExchanges {
        public int count { get; set; }
        public List<string> exchanges { get; set; }
    }
    public class RootObjectExchanges {
        public bool success { get; set; }
        public string message { get; set; }
        public ResultExchanges results { get; set; }
    }


    // Class for list of markets
    public class ResultsMarkets {
        public int count { get; set; }
        public List<string> markets { get; set; }
    }
    public class RootObjectMarkets {
        public bool success { get; set; }
        public string message { get; set; }
        public ResultsMarkets results { get; set; }
    }

    // Class for list of files
    public class FileMeta {
        public int id { get; set; }
        public string exchange { get; set; }
        public string market { get; set; }
        public string type { get; set; }
        public DateTime date { get; set; }
        public int size { get; set; }
    }
    public class ResultsFile {
        public int count { get; set; }
        public List<FileMeta> files { get; set; }
    }
    public class RootObjectFiles {
        public bool success { get; set; }
        public string message { get; set; }
        public ResultsFile results { get; set; }
    }

    // Class for trade
    public class Trade {
        public Trade(Dictionary<string, object> trade) {
            if (trade.Count != 7)
                return;
            timestamp = UInt64.Parse(trade["timestamp"].ToString(), CultureInfo.InvariantCulture);
            base_currency = trade["base_currency"].ToString();
            counter_currency = trade["counter_currency"].ToString();
            trade_time = UInt64.Parse(trade["trade_time"].ToString(), CultureInfo.InvariantCulture);
            trade_id = trade["trade_id"].ToString();
            price = double.Parse(trade["price"].ToString(), CultureInfo.InvariantCulture);
            size = double.Parse(trade["size"].ToString(), CultureInfo.InvariantCulture);
        }

        public UInt64 timestamp { get; set; }

        public string base_currency { get; set; }

        public string counter_currency { get; set; }

        public UInt64 trade_time { get; set; }

        public string trade_id { get; set; }

        public double price { get; set; }

        public double size { get; set; }
    }

    // Class for orderbook
    public class Orderbook {
        public Orderbook(Dictionary<string, object> orderbook) {
            if (orderbook.Count == 0)
                return;

            timestamp = UInt64.Parse(orderbook["timestamp"].ToString(), CultureInfo.InvariantCulture);
            base_currency = orderbook["base_currency"].ToString();
            counter_currency = orderbook["counter_currency"].ToString();
            if(orderbook["tick_size"].ToString().Length > 0)
                tick_size = double.Parse(orderbook["tick_size"].ToString(), CultureInfo.InvariantCulture);

            uint i = 0;
            Dictionary<string, List<Order>> data = new Dictionary<string, List<Order>> { };

            // For asks and bids
            foreach (string s_type in new List<string>{ "asks", "bids"}) { 
                data.Add(s_type, new List<Order> { });
                string type = s_type.Substring(0, 3);
                i = 0;
                while (orderbook.ContainsKey(type+"[" +i.ToString()+"].price"))
                {
                    Order o = new Order();
                    if (orderbook[type + "[" + i.ToString() + "].price"].ToString().Length > 1)
                        o.price = double.Parse(orderbook[type + "[" + i.ToString() + "].price"].ToString(), CultureInfo.InvariantCulture);
                    else
                        o.price = -1;

                    if (orderbook[type + "[" + i.ToString() + "].size"].ToString().Length > 1)
                        o.size = double.Parse(orderbook[type + "[" + i.ToString() + "].size"].ToString(), CultureInfo.InvariantCulture);
                    else
                        o.size = -1;

                    if(o.size != -1 && o.price != -1)
                        data[s_type].Add(o);
                    i++;
                }
            }

            // Store data
            orderbooks = data;
        }

        public UInt64 timestamp { get; set; }
        public string base_currency { get; set; }
        public string counter_currency { get; set; }
        public double tick_size { get; set; }
        public Dictionary<string, List<Order>> orderbooks { get; set; }

        public struct Order {
            public double price;
            public double size;
        }
    }
}
