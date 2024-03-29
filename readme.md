# Libraries for MarketLab
Website: https://www.market-lab.app  
Documentation: https://www.market-lab.app/documentation  
Swagger: https://www.market-lab.app/swagger

Libraries for:
* Python3
* .NET

## .NET

### Install
Find and install the [MarketLab](https://www.nuget.org/packages/MarketLab/) NuGet package into your Project.  See [more help](https://www.google.com/search?q=install+nuget+package) for installing packages;
``` PowerShell
PM> Install-Package MarketLab
```

### Example usage in C#
``` csharp
// These fonctions must be used in a real project with class, etc.

using MarketLab;

// Init lib
MarketLabAPI ml = new MarketLabAP ('{YOUR_API_KEY}');

// Get list of exchanges
RootObjectExchanges list_exchanges = ml.get_exchanges();

// Get list of markets for an exchange
RootObjectMarkets list_markets = ml.get_markets('binance');

// Get information about a market
RootObjectInformationMarket info_market = ml.get_information_market('binance','eth_btc');

// Init replay, set false at the end to start it
ml.init_replay(callback, 'binance', 'eth_btc', '2020-04-06', '2020-04-08', 'trade', stuffDone, true);

// Callback on event (trade and orderbook)
void callback(Trade last_trade, Orderbook last_orderbook, MarketLabAPI.Event_type last_event_type) {
    // Add your trading algorithm here

    // Sample to get the price of the better ask and bid:
    if(last_orderbook != null){
        double best_ask = last_orderbook.orderbooks["asks"][0].price;
        double best_bid = last_orderbook.orderbooks["bids"][0].price;
    }

    // Event_type indicates if the last event is a trade, an orderbook or the end of replay.

    // For exemple, you can call a class to do an action and get the returned object in variable
    // Your EventHandler will be call by the library
    object data = DoStuff();
    return data ;
}

// Function call on the EventHandler
private void stuffDone(object data, EventArgs e){
     Console.WriteLine(data.ToString());
}
```

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