import requests
import pandas as pd
import os
from datetime import datetime, timedelta

class MarketLab:
    _BASE_API_ = 'https://api.marketlab.app/1/data.php'
    _COLUMNS_TRADE_ = {'timestamp': 'int64', 'base_currency': 'category', 'counter_currency': 'category', 'trade_time':'int64', 'trade_id':'int64', 'price':'float64', 'size':'float64'}
    _COLUMNS_ORDERBOOK_ = {'timestamp': 'int64', 'base_currency': 'category', 'counter_currency': 'category', 'tick_size': 'float64', 'ask[0].price': 'float64', 'ask[0].size': 'float64', 'bid[0].price': 'float64', 'bid[0].size': 'float64', 'ask[1].price': 'float64', 'ask[1].size': 'float64', 'bid[1].price': 'float64', 'bid[1].size': 'float64', 'ask[2].price': 'float64', 'ask[2].size': 'float64', 'bid[2].price': 'float64', 'bid[2].size': 'float64', 'ask[3].price': 'float64', 'ask[3].size': 'float64', 'bid[3].price': 'float64', 'bid[3].size': 'float64', 'ask[4].price': 'float64', 'ask[4].size': 'float64', 'bid[4].price': 'float64', 'bid[4].size': 'float64', 'ask[5].price': 'float64', 'ask[5].size': 'float64', 'bid[5].price': 'float64', 'bid[5].size': 'float64', 'ask[6].price': 'float64', 'ask[6].size': 'float64', 'bid[6].price': 'float64', 'bid[6].size': 'float64', 'ask[7].price': 'float64', 'ask[7].size': 'float64', 'bid[7].price': 'float64', 'bid[7].size': 'float64', 'ask[8].price': 'float64', 'ask[8].size': 'float64', 'bid[8].price': 'float64', 'bid[8].size': 'float64', 'ask[9].price': 'float64', 'ask[9].size': 'float64', 'bid[9].price': 'float64', 'bid[9].size': 'float64', 'ask[10].price': 'float64', 'ask[10].size': 'float64', 'bid[10].price': 'float64', 'bid[10].size': 'float64', 'ask[11].price': 'float64', 'ask[11].size': 'float64', 'bid[11].price': 'float64', 'bid[11].size': 'float64', 'ask[12].price': 'float64', 'ask[12].size': 'float64', 'bid[12].price': 'float64', 'bid[12].size': 'float64', 'ask[13].price': 'float64', 'ask[13].size': 'float64', 'bid[13].price': 'float64', 'bid[13].size': 'float64', 'ask[14].price': 'float64', 'ask[14].size': 'float64', 'bid[14].price': 'float64', 'bid[14].size': 'float64', 'ask[15].price': 'float64', 'ask[15].size': 'float64', 'bid[15].price': 'float64', 'bid[15].size': 'float64', 'ask[16].price': 'float64', 'ask[16].size': 'float64', 'bid[16].price': 'float64', 'bid[16].size': 'float64', 'ask[17].price': 'float64', 'ask[17].size': 'float64', 'bid[17].price': 'float64', 'bid[17].size': 'float64', 'ask[18].price': 'float64', 'ask[18].size': 'float64', 'bid[18].price': 'float64', 'bid[18].size': 'float64', 'ask[19].price': 'float64', 'ask[19].size': 'float64', 'bid[19].price': 'float64', 'bid[19].size': 'float64'}
    
    def __init__(self, apiKey:str = None):
        self.apiKey = apiKey
    
    def init_replayer(self, callback, exchange: str, market: str, start_date: str, end_date: str, type: str = None, callback_error=None):
        self.callback = callback
        self.callback_error = callback_error
        self.exchange = exchange
        self.market = market
        self.start_date = start_date
        self.end_date = end_date
        self.type = type
        self.dates = self.__get_dates_to_replay(self.start_date, self.end_date)
        
        # Doawload usefull file
        status_download = self.__download_files(self.exchange, self.market, self.start_date, self.end_date, self.type)
        if(not status_download):
            return
        
        # Init variable
        orderbooks = []
        trades = []
        last_orderbook = []
        last_trade = []
        # For each date selected
        for date in self.dates:
            
            # Read df
            if(self.type is None or self.type == 'orderbook'):
                orderbooks = self.__read_df('cache-ml/{}/{}/{}/{}.csv.gz'.format(self.exchange, 'orderbook', self.market, date), 'orderbook')
                if(orderbooks is None):
                    self.on_error('File cache-ml/{}/{}/{}/{}.csv.gz doesn\'t exist. Make sure a file exist for this exchange, market and date using API'.format(self.exchange, 'orderbook', self.market, date))
                    continue
            if(self.type is None or self.type == 'trade'):
                trades = self.__read_df('cache-ml/{}/{}/{}/{}.csv.gz'.format(self.exchange, 'trade', self.market, date), 'trade')
                if(trades is None):
                    self.on_error('File cache-ml/{}/{}/{}/{}.csv.gz doesn\'t exist. Make sure a file exist for this exchange, market and date using API'.format(self.exchange, 'trade', self.market, date))
                    continue
              
            events = [*orderbooks, *trades]            
            
            # Read event and callback each        
            for event in sorted(events, key=lambda e: e['timestamp']):
                # If trade
                if(event.get('ask[0].price', None) == None):
                    last_trade = event
                # If orderbook
                else:
                    last_orderbook = event
                # Callback
                self.callback(last_orderbook, last_trade)
            
            # Free memory
            del events
            del orderbooks
            del trades
    
    def __read_df(self, filename: str, type: str):
        if(not os.path.exists(filename)):
            print('file ' + filename + ' not found')
            return None
        if(type == 'trade'):
            return pd.read_csv(filename, compression='gzip', sep='\t', quotechar='"', dtype=MarketLab._COLUMNS_TRADE_).to_dict('records')
        elif(type == 'orderbook'):
            return pd.read_csv(filename, compression='gzip', sep='\t', quotechar='"', dtype=MarketLab._COLUMNS_ORDERBOOK_).to_dict('records')
        return None
    
    # Get list of files matching to the parameters
    def get_files(self, exchange: str, market: str, start_date: str, end_date: str, type: str = None):
        try:
            get = {
                'exchange': exchange,
                'market': market, 
                'start_date': start_date,
                'end_date': end_date
            }
            if type is not None:
                get['type'] = type
                
            data = requests.get(MarketLab._BASE_API_ + '?list_files', params=get).json()
            if(data['success'] == True):
                return data['results']['files']
            self.on_error(data['message'])
            return None
        except Exception as err:
            print(err)
            return []
       
    # Get list of exhanges
    def get_exchanges(self):
        try:
            data = requests.get(MarketLab._BASE_API_ + '?list_exchanges').json()
            if(data['success'] == True):
                return data['results']['exchanges']
            self.on_error(data['message'])
            return None
        except Exception as err:
            print(err)
            return []
        
    # Get list of markets for an exchange
    def get_markets(self, exchange: str):
        try:
            get = {
                'exchange': exchange
            }
            data = requests.get(MarketLab._BASE_API_ + '?list_markets', params=get).json()
            if(data['success'] == True):
                return data['results']['markets']
            self.on_error(data['message'])
            return None
        except Exception as err:
            print(err)
            return []
    
    # Get information about market
    def info_market(self, exchange: str, market: str):
        try:
            get = {
                'exchange': exchange,
                'market': market
            }
            data = requests.get(MarketLab._BASE_API_ + '?info_market', params=get).json()
            if(data['success'] == True):
                return data['results']
            self.on_error(data['message'])
            return None
        except Exception as err:
            print(err)
            return []  
    
    # Return df from file (download or find in cache)
    def __download_files(self, exchange: str, market: str, start_date: str, end_date: str, type: str = None):   
        # Download files usefull between params dates
        files = self.get_files(exchange, market, start_date, end_date, type)
        
        for file in files:
            # Try to download file in not in cache
            try:
                output_file = 'cache-ml/{}/{}/{}/{}.csv.gz'.format(file['exchange'], file['type'], file['market'], file['date'])
                if(not os.path.exists(output_file)):
                    get = {
                        'api_key':self.apiKey,
                        'exchange': file['exchange'],
                        'market': file['market'],
                        'type': file['type'],
                        'date':file['date']
                    }
                    req = requests.get(MarketLab._BASE_API_ + '?file', params=get, allow_redirects=True)
                    if(req.status_code == 200):
                        self.__create_subdirectory(output_file)
                        open(output_file, 'wb').write(req.content)
                    else:
                        self.on_error(req.content)
                        return False
            except Exception as err:
                self.on_error(req.content)
                return False
        return True
            
    # On download file, create subdirectory for the cache
    def __create_subdirectory(self, filename):
        if not os.path.exists(os.path.dirname(filename)):
            try:
                os.makedirs(os.path.dirname(filename))
            except Exception as err:
                print('Error creating directory: ' + err)
           
    # Return an array with all date to replay     
    def __get_dates_to_replay(self, start_date: str, end_date: str):
        start_d = datetime.strptime(start_date, '%Y-%m-%d')
        end_d = datetime.strptime(end_date, '%Y-%m-%d')
        tmp_d = datetime.strptime(start_date, '%Y-%m-%d')
        res = []
        while(tmp_d <= end_d):
            res.append(str(tmp_d.strftime('%Y-%m-%d')))
            tmp_d += timedelta(days=1)
        return res
    
    # Call on error
    def on_error(self, data):
        if(self.callback_error):
            self.callback_error(data)
        else:
            print(data)
            
        
        