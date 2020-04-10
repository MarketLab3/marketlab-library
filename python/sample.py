import maketlab

class Sample:
    def __init__(self):
        # Init lib with api key, exhchange and market
        self.ml = maketlab.MarketLab('{YOUR_API_KEY}')
        
        # Call samples
        print(self.ml.get_exchanges())
        print(self.ml.info_market('binance', 'eth_btc'))
        print(self.ml.get_markets('binance'))
        
        # Replay sample (Type=None means get Trades and Orderbooks)
        self.ml.init_replayer(self.event, 'binance', 'eth_btc', '2020-04-06', '2020-04-08', None, self.event_error)
       
    # Replay  On event - callback
    def event(self, last_orderbook, last_trade):
        print(last_orderbook)
        print(last_trade)
        
    # On event error
    def event_error(self, msg):
        print('event_error - ' + str(msg))
        
if __name__ == '__main__':
    Sample()