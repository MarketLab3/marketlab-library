using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using RestSharp;
using System.Data;
using LumenWorks.Framework.IO.Csv;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Core;
using System.Threading;

namespace MarketLab {
    public class MarketLabAPI {

        // Variables globales
        private const string _BASE_API_ = "https://api.market-lab.app/1";
        private bool show_errors = false;
        private string apiKey = null;
        private RestClient client = new RestClient();
        private List<string> errors = new List<string>();
        private Thread thread_replay = null;

        /// <summary>
        /// Event type during the replaying
        /// </summary>
        public enum Event_type : uint{
            TRADE = 1,
            ORDERBOOK = 2,
            END = 3
        }

        /// <summary>
        /// Get information if it's replaying
        /// </summary>
        public bool is_replaying {
            get {
                if (thread_replay != null)
                    return thread_replay.IsAlive;
                return false;
            }
        }
        
        public delegate void CallBack(Trade last_trade, Orderbook last_orderbook, Event_type last_event_type);
        
        private CallBack callback = null;
        private string replay_exchange = null;
        private string replay_market = null;
        private string replay_type = null;
        private string replay_start_date = null;
        private string replay_end_date = null;


        /// <summary>
        /// Initialize the replay
        /// </summary>
        /// <param name="apiKey">Your API key (find it in your account on market-lab.app)</param>
        /// <param name="show_errors">Show the error with popup during the replay</param>
        public MarketLabAPI(string apiKey = null, bool show_errors = true) {
            this.apiKey = apiKey;
            this.show_errors = show_errors;
            client.BaseUrl = new Uri(_BASE_API_);
            client.AddDefaultHeader("Accept", "application/json");
        }

        /// <summary>
        /// Add API key if init without
        /// </summary>
        /// <param name="apiKey">Your API key (find it in your account on market-lab.app)</param>
        public void add_api_key(string apiKey) {
            this.apiKey = apiKey;
        }

        /// <summary>
        /// Get information about a market
        /// </summary>
        /// <param name="exchange">name of exchange</param>
        /// <param name="market">name of market</param>
        /// <returns>Object with status of request and all informations about a market</returns>
        public RootObjectInformationMarket get_information_market(string exchange, string market) {

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("exchange", exchange);
            parameters.Add("market", market);
            try
            {
                string json = this.execute_request("info_market", parameters);
                if(json != null)
                    return JsonConvert.DeserializeObject<RootObjectInformationMarket>(json);
                return null;
            }
            catch (Exception ex)
            {
                this.add_error("get_information_market - " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get list of exchanges
        /// </summary>
        /// <returns>Object with status of request and the list of exchanges</returns>
        public RootObjectExchanges get_exchanges() {

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            try
            {
                string json = this.execute_request("list_exchanges", parameters);
                if (json != null)
                    return JsonConvert.DeserializeObject<RootObjectExchanges>(json);
                return null;
            }
            catch (Exception ex)
            {
                this.add_error("get_exchanges - " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get list of markets for the exchange
        /// </summary>
        /// <param name="exchange">Object with status of request and the list of markets</param>
        /// <returns></returns>
        public RootObjectMarkets get_markets(string exchange) {

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("exchange", exchange);

            try
            {
                string json = this.execute_request("list_markets", parameters);
                if (json != null)
                    return JsonConvert.DeserializeObject<RootObjectMarkets>(json);
                return null;
            }
            catch (Exception ex)
            {
                this.add_error("get_markets - " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get list of files for the exchange, markets, dates and type
        /// </summary>
        /// <param name="exchange">Name of the exchange</param>
        /// <param name="market">Name of the market</param>
        /// <param name="start_date">Date to start 'YYYY-MM-DD'</param>
        /// <param name="end_date">Date to end 'YYYY-MM-DD'</param>
        /// <param name="type">Type of data ('trade' or 'orderbook'. Set to null for both)</param>
        /// <returns></returns>
        public RootObjectFiles get_files(string exchange, string market, string start_date, string end_date, string type = null) {

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("exchange", exchange);
            parameters.Add("market", market);
            parameters.Add("start_date", start_date);
            parameters.Add("end_date", end_date);
            if (type != null)
                parameters.Add("type", type);

            try
            {
                string json = this.execute_request("list_files", parameters);
                if (json != null)
                    return JsonConvert.DeserializeObject<RootObjectFiles>(json);
                return null;
            }
            catch (Exception ex)
            {
                this.add_error("get_files - " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Init replay the replay
        /// </summary>
        /// <param name="callback">Callback function</param>
        /// <param name="exchange">Name of exchange to replay</param>
        /// <param name="market">Name of market to replay</param>
        /// <param name="start_date">Date to start the replay</param>
        /// <param name="end_date">Date to end the replay</param>
        /// <param name="type">Type of data to replay ('trade' or 'orderbook'. Set to null for both)</param>
        /// <param name="start">Set to true to start the replay directly</param>
        /// <returns></returns>
        public bool init_replay(CallBack callback, string exchange, string market, string start_date, string end_date, string type = null, bool start = false) {
            // Init data
            this.callback = new CallBack(callback);
            this.replay_exchange = exchange;
            this.replay_market = market;
            this.replay_start_date = start_date;
            this.replay_end_date = end_date;
            this.replay_type = type;

            if (start == true)
                start_replay();

            return true;
        }

        /// <summary>
        ///  Start the replay. Must be init before.
        /// </summary>
        public void start_replay() {
            if (thread_replay == null || thread_replay.IsAlive == false)
            {
                thread_replay = new Thread(work_replay);
                thread_replay.Start();
            }
            else
                thread_replay.Resume();
        }

        /// <summary>
        /// Stop or pause the replat
        /// </summary>
        /// <param name="definitely">definitely true means stop the replay.</param>
        public void stop_replay(bool definitely = true) {
            if(thread_replay != null && thread_replay.IsAlive)
            {
                try { 
                if (definitely == true) {
                    thread_replay.Resume();
                    thread_replay.Abort();
                }
                else
                    thread_replay.Suspend();
                }
                catch
                {
                    thread_replay.Abort();
                }
            }
        }
        // Work for the replay in new thread
        private void work_replay() {
            // List of date to replay
            List<string> dates_replayed = this.list_dates_replayed(this.replay_start_date, this.replay_end_date);

            // Download usefull files
            if (!this.download_files(this.replay_exchange.ToLower(), this.replay_market.ToLower(), this.replay_start_date, this.replay_end_date, this.replay_type))
                return;

            // Init variables
            Dictionary<string, object> last_trade = new Dictionary<string, object>();
            Dictionary<string, object> last_orderbook = new Dictionary<string, object>();
            Event_type last_event_type;

            // For each file, read
            foreach (string date in dates_replayed)
            {
                FileInfo file_trade = new FileInfo("./cache-ml/" + this.replay_exchange + "/trade/" + this.replay_market + "/" + date + ".csv.gz");
                FileInfo file_orderbook = new FileInfo("./cache-ml/" + this.replay_exchange + "/orderbook/" + this.replay_market + "/" + date + ".csv.gz");
                // Tests files
                if ((this.replay_type == null || this.replay_type == "trade") && file_trade.Exists == false)
                {
                    add_error("init_replay - file trade " + file_trade.FullName + " doesn't exist.");
                    return;
                }
                // Tests files
                if ((this.replay_type == null || this.replay_type == "orderbook") && file_orderbook.Exists == false)
                {
                    add_error("init_replay - file orderbook " + file_orderbook.FullName + " doesn't exist.");
                    return;
                }

                // Read files
                List<KeyValuePair<ulong, object>> df_trade = new List<KeyValuePair<ulong, object>>();
                List<KeyValuePair<ulong, object>> df_orderbook = new List<KeyValuePair<ulong, object>>();
                List<KeyValuePair<ulong, object>> df_events = new List<KeyValuePair<ulong, object>>();
                if (this.replay_type == null || this.replay_type == "trade")
                    this.read_df(file_trade, out df_trade);
                if (this.replay_type == null || this.replay_type == "orderbook")
                    this.read_df(file_orderbook, out df_orderbook);

                // Sort trades and orderbooks
                df_events.AddRange(df_orderbook);
                df_events.AddRange(df_trade);
                df_trade = null;
                df_orderbook = null;
                df_events.Sort((x, y) => x.Key.CompareTo(y.Key));

                // For each event
                foreach (KeyValuePair<ulong, object> event_ in df_events)
                {
                    Dictionary<string, object> event_parse = (Dictionary<string, object>)event_.Value;
                    // If trade
                    if (event_parse.ContainsKey("ask[0].price") == false)
                    {
                        last_trade = event_parse;
                        last_event_type = Event_type.TRADE;
                    }
                    // If orderbook
                    else
                    {
                        last_orderbook = event_parse;
                        last_event_type = Event_type.ORDERBOOK;
                    }

                    // Callback
                    this.callback(new Trade(last_trade), new Orderbook(last_orderbook), last_event_type);

                }
                this.free_object(df_events);
            }
            // End of replay
            this.callback(null, null, Event_type.END);
            return;
        }

        // Read file
        private void read_df(FileInfo fileName, out List<KeyValuePair<ulong, object>> df) {
            

            string currentFileName = fileName.FullName;
            string tmp_output = "./cache-ml/tmp.csv";

            if (File.Exists(tmp_output))
                File.Delete(tmp_output);

            byte[] dataBuffer = new byte[4096];
            using (System.IO.Stream fs = new FileStream(currentFileName, FileMode.Open, FileAccess.Read))
            {
                using (GZipInputStream gzipStream = new GZipInputStream(fs))
                {                    
                    using (FileStream fsOut = File.Create(tmp_output))
                    {
                        StreamUtils.Copy(gzipStream, fsOut, dataBuffer);
                    }
                }
            }

            DataTable csvTable = new DataTable();
            CsvReader csvReader = new CsvReader(new StreamReader(File.OpenRead(tmp_output)), true, '\t');

            string[] header = csvReader.GetFieldHeaders();
            csvTable.Load(csvReader);
            csvReader.Dispose();
            csvReader = null;

            df = new List<KeyValuePair<ulong, object>>();
            for (int i = 0; i < csvTable.Rows.Count; i++)
            {
                Dictionary<string, object> tmp_object = new Dictionary<string, object>();
                for (int j = 0; j < header.Length; j++)
                {
                    tmp_object.Add(header[j], csvTable.Rows[i][j]);
                }
                df.Add(new KeyValuePair<ulong, object>(ulong.Parse(csvTable.Rows[i][0].ToString()), tmp_object));
                tmp_object = null;
            }

            csvTable.Dispose();
            csvTable = null;
                
        }

        // Send a request to the API
        private string execute_request(string ressources, Dictionary<string, string> parameters) {
            try
            {
                // Create request
                RestRequest request = new RestRequest("data.json", Method.GET);
                request.AddParameter(ressources, null);
                foreach (KeyValuePair<string, string> kvp in parameters)
                {
                    request.AddParameter(kvp.Key, kvp.Value);
                }

                // Execute request
                IRestResponse response = client.Execute(request);
                // If error
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    this.add_error(response.Content, true);
                    return null;
                }
                
                return response.Content.Remove(0, 1);
            }
            catch (Exception ex)
            {
                this.add_error("Fail request. catch:  " + ex.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// Download files for indaicated date
        /// </summary>
        /// <param name="exchange">Name of exchange to download</param>
        /// <param name="market">Name of market to download</param>
        /// <param name="start_date">Date of the first file to downlaod</param>
        /// <param name="end_date">Date of the last file to downlaod</param>
        /// <param name="type">Type of data ('trade' or 'orderbook'. Set to null for both)</param>
        /// <returns></returns>
        public bool download_files(string exchange, string market, string start_date, string end_date, string type = null) {
            try
            {
                // Use API to get list of files
                RootObjectFiles list_files = this.get_files(exchange, market, start_date, end_date, type);
                FileInfo output_path = null;

                if (list_files == null || list_files.success == false || list_files.results.files == null)
                {
                    add_error("Fail downloading files:  " + list_files.message);
                    return false;
                }

                // For each file
                foreach (FileMeta file in list_files.results.files)
                {
                    // Output file
                    output_path = new FileInfo("./cache-ml/" + file.exchange + "/" + file.type + "/" + file.market + "/" + file.date.ToString("yyyy-MM-dd") + ".csv.gz");
                    // If exists, doesn't download
                    if (output_path.Exists)
                        continue;
                    // File directory doesn't exist, create
                    if (!Directory.Exists(output_path.DirectoryName))
                        Directory.CreateDirectory(output_path.DirectoryName);

                    // Prepare request
                    RestRequest request = new RestRequest("data.json", Method.GET);
                    request.AddParameter("file", null);
                    request.AddParameter("api_key", this.apiKey);
                    request.AddParameter("exchange", file.exchange);
                    request.AddParameter("market", file.market);
                    request.AddParameter("date", file.date.ToString("yyyy-MM-dd"));
                    request.AddParameter("type", file.type);

                    IRestResponse response = client.Execute(request);
                    if(response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        add_error(response.Content, true);
                        return false;
                    }

                    // Downlaod byte
                    byte[] data = client.DownloadData(request);

                    // Save byte and close file
                    FileStream output = new FileStream(output_path.FullName, FileMode.Create);
                    output.Write(data, 0, data.Length);
                    output.Flush();
                    output.Close();
                    data = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                add_error("download_files - error during files download: " + ex.Message);
                return false;
            }
        }

        // List of dates to raplay
        private List<string> list_dates_replayed(string start, string end) {
            DateTime start_date = DateTime.ParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateTime tmp_date = DateTime.ParseExact(start, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateTime end_date = DateTime.ParseExact(end, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            List<string> res = new List<string>();

            // Test dates
            if (start_date > end_date)
            {
                add_error("start_date must be before end_date");
                return res;
            }

            // Create list of dates
            while (tmp_date <= end_date)
            {
                res.Add(tmp_date.ToString("yyyy-MM-dd"));
                tmp_date = tmp_date.AddDays(1);
            }

            return res;
        }

        // Add error to the stack
        private void add_error(string data, bool deserializable = false) {
            RootObjectError error = null;
            if (deserializable == true)
                error = JsonConvert.DeserializeObject<RootObjectError>(data.Remove(0, 1));
            
            errors.Add(data);
            if (this.show_errors) { 
                Console.WriteLine(data);
                if(error!=null)
                    MessageBox.Show(error.message, "Error API", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show(data, "Error API", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return;
        }

        /// <summary>
        /// Get the list of errors and clear it
        /// </summary>
        /// <returns>List of string errors</returns>
        public List<string> get_errors() {
            List<string> tmp = errors;
            errors.Clear();
            return tmp;
        }

        // Force to free memory for a list
        private void free_object(object obj) {
            int identificador = GC.GetGeneration(obj);
            obj = null;
            GC.Collect(identificador, GCCollectionMode.Forced);
        }
    }

}