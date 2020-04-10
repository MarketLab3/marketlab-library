# Libraries for MarketLab
Website: https://www.marketlab.app  
Documentation: https://www.marketlab.app/documentation  
Swagger: https://www.marketlab.app/swagger

## Python
Download python lib from this repository. Then:
```python
import maketlab

# Init lib with api key
ml = maketlab.MarketLab('{YOUR_API_KEY}')

# Init the raplayer with the parameters - Optional: {TYPE} and callback on_error
ml.init_replayer(event, '{EXCHANGE}', '{MARKET}', '{START_DATE}', '{END_DATE}', '{TYPE}', on_error))

# On event - callback
def event(last_orderbook, last_trade):
    print(last_orderbook)
    print(last_trade)
    # add you algorithm here to backtest your strategy

# On event error
def on_error(message):
    print(message) # quota reached, data not avaible, plan inactive, etc.
```

## .NET - C#
Comming soon