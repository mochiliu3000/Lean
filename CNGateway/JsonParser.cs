using System;
using System.Web.Script.Serialization;
using QuantConnect;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using System.Collections.Generic;

namespace CNGateway
{
    /// <summary>
    /// convert json msg to Quantconnect Tick format
    /// </summary>
    public class JsonParser
    {
        private String source { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:CNGateway.JsonParser"/> class.
        /// </summary>
        /// <param name="source">used to decide data format</param>
        public JsonParser(String source)
        {
            //source can be "CTP", "sina", "tencent" etc
            this.source = source;
        }

        /// <summary>
        /// Parses the json.
        /// </summary>
        /// <returns>new QC Tick ojb</returns>
        /// <param name="strMessage">String message.</param>
        /// <param name="routingKey">msg key, which contains data source name, </param>
        public Tick ParseJson(string strMessage, string routingKey)
        {

            //Log.Trace("Inside parser to parse...");
            JavaScriptSerializer json_serializer = new JavaScriptSerializer();

            Tick newtick;
            switch (this.source)
            {
                case "sina":
                case "tencent":
                    SinaJson obj = json_serializer.Deserialize<SinaJson>(strMessage);
                    obj.code = routingKey.Split('.')[1];
                    //Console.WriteLine("[x] Tick Created: " + obj);
                    newtick = obj.ToTick();   

                    break;
                case "vnpy":
                    VnpyJson vJson = json_serializer.Deserialize<VnpyJson>(strMessage);
                    //Console.WriteLine(vJson);
                    newtick = vJson.ToTick();   
                    Console.WriteLine(newtick);
                    break;

                case "coin":
                    CoinJson cJson = json_serializer.Deserialize<CoinJson>(strMessage);
                    //Console.WriteLine(vJson);
                    newtick = cJson.ToTick();
                    //Console.WriteLine(newtick);
                    break;
                case "crypto":
                    CryptoJson crJson = json_serializer.Deserialize<CryptoJson>(strMessage);
                    //Console.WriteLine(vJson);
                    newtick = crJson.ToTick();
                    //Console.WriteLine(newtick);
                    break;



                default:
                    newtick = new Tick();
                    break;
            }

            Console.WriteLine("QC tick: Symbol:'{0}' Time: '{1}' Quantity:'{2}'", 
                              newtick.Symbol.Value, newtick.Time.ToString(), newtick.Quantity);

            return newtick;

        }
    }


    public class CryptoJson
    {

        public decimal sell { get; set; }
        public decimal buy { get; set; }
        public decimal last { get; set; }
        public decimal vol { get; set; }
        public decimal high { get; set; }
        public decimal low { get; set; }

        public Tick ToTick()
        {
            Tick newtick = new Tick();
            try
            {
                newtick = new Tick()
                {
                    Symbol = QuantConnect.Symbol.Create("eth", SecurityType.Equity, "crypto"),
                    Time = DateTime.UtcNow,
                    Value = last,
                    BidPrice = 0.0m,
                    AskPrice = 1.0m,
                    Exchange = "",
                    SaleCondition = "",
                    Quantity = 1.0m,
                    Suspicious = false,
                    DataType = MarketDataType.Tick,
                    TickType = TickType.Trade,
                    BidSize = 1.0m,
                    AskSize = 1.0m


                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Json convertion error:" + ex.Message);
            }

            return newtick;
        }

        public override String ToString()
        {
            String obj = "symbol:" + "eth" + ", " +
                "vol:" + vol + ", " +
                "last:" + last;
            return obj;

        }
    }

    /// <summary>
    /// Jason data from Sina .
    /// </summary>
    public class SinaJson
    {

        public String code { get; set; }
        public String name { get; set; }
        public decimal buy { get; set; }
        public decimal sell { get; set; }
        public decimal now { get; set; }
        public decimal open { get; set; }
        public decimal close { get; set; }
        public decimal high { get; set; }
        public decimal low { get; set; }
        public decimal turnover { get; set; }
        public decimal volume { get; set; }
        public decimal ask1 { get; set; }
        public decimal ask1_volume { get; set; }
        public decimal ask2 { get; set; }
        public decimal ask2_volume { get; set; }
        public decimal ask3 { get; set; }
        public decimal ask3_volume { get; set; }
        public decimal ask4 { get; set; }
        public decimal ask4_volume { get; set; }
        public decimal ask5 { get; set; }
        public decimal ask5_volume { get; set; }
        public decimal bid1 { get; set; }
        public decimal bid1_volume { get; set; }
        public decimal bid2 { get; set; }
        public decimal bid2_volume { get; set; }
        public decimal bid3 { get; set; }
        public decimal bid3_volume { get; set; }
        public decimal bid4 { get; set; }
        public decimal bid4_volume { get; set; }
        public decimal bid5 { get; set; }
        public decimal bid5_volume { get; set; }
        public String date { get; set; }
        public String time { get; set; }

        //name = ""; //# 股票名
        //buy = 0;//# 竞买价
        //sell = 0;//# 竞卖价
        //now = 0;//# 现价
        //open = 0;//# 开盘价
        //close = 0;//# 昨日收盘价
        //high = 0;//# 今日最高价
        //low = 0;//# 今日最低价
        //turnover = 0;//# 交易股数
        //volume = 0; // # 交易金额

        public Tick ToTick (){
            Tick newtick = new Tick();
            try{    
                newtick = new Tick()
                {
                    Symbol = Symbol.Create(code, SecurityType.Equity, "cn"),
                    Time = Convert.ToDateTime(date + " " + time),
                    Value = close,
                    BidPrice = bid1,
                    AskPrice = ask1,
                    Exchange = "",
                    SaleCondition = "",
                    Quantity = volume,
                    Suspicious = false,
                    DataType = MarketDataType.Tick,
                    TickType = TickType.Trade,
                    BidSize = bid1_volume,
                    AskSize = ask1_volume

                    //Symbol = Symbol.Create(code, SecurityType.Equity, "cn"),
                    //Time = DateTime.Now,
                    //Value = 1,
                    //BidPrice = 2,
                    //AskPrice = 3,
                    //Exchange = "",
                    //SaleCondition = "",
                    //Quantity = 4,
                    //Suspicious = false,
                    //DataType = MarketDataType.Tick,
                    //TickType = TickType.Quote,
                    //BidSize = 5,
                    //AskSize = 6
                };
            }catch(Exception ex){
                Console.WriteLine("Json convertion error:"+ex.Message);
            }

            return  newtick; 
        }

        public override String ToString (){
            String obj = "name:" + name + ", "+
                "code:" + code + ", " +
                "time:" + time + ", " +
                "buy:" + buy + ", " +
                "sell:" + sell + ", " +
                "now:" +now + ", "+
                "ask1:" + ask1 + ", " +
                "bid1:" + bid1 + ", " +
                "volume:" +volume; 
            return obj;
            
        }
    }



    public class VnpyJson
    {

        //# 代码相关
        public string symbol { get; set; }             //# 合约代码
        public string exchange { get; set; }           //# 交易所代码
        public decimal vtSymbol { get; set; }           //# 合约在vt系统中的唯一代码，通常是 合约代码.交易所代码
        
        //# 成交数据
        public decimal lastPrice { get; set; }            //# 最新成交价
        public int lastVolume { get; set; }             //# 最新成交量
        public int volume { get; set; }                 //# 今天总成交量
        public int openInterest { get; set; }           //# 持仓量
        public string time { get; set; }               //# 时间 11:20:56.5
        public string date { get; set; }               //# 日期 20151009
        public string datetime { get; set; }                    //# python的datetime时间对象
        
        //# 常规行情
        public decimal openPrice { get; set; }            //# 今日开盘价
        public decimal highPrice { get; set; }            //# 今日最高价
        public decimal lowPrice { get; set; }             //# 今日最低价
        public decimal preClosePrice { get; set; }
        
        public decimal upperLimit { get; set; }           //# 涨停价
        public decimal lowerLimit { get; set; }           //# 跌停价
        
        //# 五档行情
        public decimal bidPrice1 { get; set; }
        public decimal bidPrice2 { get; set; }
        public decimal bidPrice3 { get; set; }
        public decimal bidPrice4 { get; set; }
        public decimal bidPrice5 { get; set; }
        
        public decimal askPrice1 { get; set; }
        public decimal askPrice2 { get; set; }
        public decimal askPrice3 { get; set; }
        public decimal askPrice4 { get; set; }
        public decimal askPrice5 { get; set; }        
        
        public int bidVolume1 { get; set; }
        public int bidVolume2 { get; set; }
        public int bidVolume3 { get; set; }
        public int bidVolume4 { get; set; }
        public int bidVolume5 { get; set; }
        
        public int askVolume1 { get; set; }
        public int askVolume2 { get; set; }
        public int askVolume3 { get; set; }
        public int askVolume4 { get; set; }
        public int askVolume5 { get; set; }

        public Dictionary<string, Symbol> symbolDict = new Dictionary<string, Symbol>();

        public Tick ToTick()
        {
            Tick newtick = new Tick()
            {
                Symbol = getSymbol(symbol),
                Time = Convert.ToDateTime(date + " " + time),
                Value = lastPrice,
                BidPrice = bidPrice1,
                AskPrice = askPrice1,
                Exchange = "",
                SaleCondition = "",
                Quantity = volume,
                Suspicious = false,
                DataType = MarketDataType.Tick,
                TickType = TickType.Trade,
                BidSize = bidVolume1,
                AskSize = askVolume1
            };
            return newtick;
        }

        public Symbol getSymbol (string symbol){
            // create new one if not in the dict
            Symbol newSymbol;
            if(symbolDict.ContainsKey(symbol)){
                newSymbol = symbolDict[symbol];                
            }else{
                newSymbol = Symbol.Create(symbol, SecurityType.Equity, "cn");
            }

            return newSymbol;
        }

        public override String ToString()
        {
            String obj = "vtSymbol:" + vtSymbol + ", " +
                "lastPrice:" + lastPrice + ", " +
                "lastVolume:" + lastVolume + ", " +
                "date:" + date + ", " +
                "time:" + time + ", " +
                "openInterest:" + openInterest + ", " +
                "preClosePrice:" + preClosePrice;
            return obj;

        }
    }


    public class CoinJson
    {
        
        public decimal BidSize { get; set; }
        public String Symbol { get; set; }
        public decimal LastPrice { get; set; }
        public decimal Quantity { get; set; }


        public Tick ToTick()
        {
            Tick newtick = new Tick();
            try
            {
                newtick = new Tick()
                {
                    Symbol = QuantConnect.Symbol.Create(Symbol, SecurityType.Equity, Market.USA),
                    Time = DateTime.UtcNow,
                    Value = LastPrice,
                    BidPrice = 1.0m,
                    AskPrice = 200000.0m,
                    Exchange = "",
                    SaleCondition = "",
                    Quantity = Quantity,
                    Suspicious = false,
                    DataType = MarketDataType.Tick,
                    TickType = TickType.Trade,
                    BidSize = BidSize,
                    AskSize = 1.0m


                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Json convertion error:" + ex.Message);
            }

            return newtick;
        }

        public override String ToString()
        {
            String obj = "symbol:" + Symbol + ", " +
                "Quantity:" + Quantity + ", " +
                "LastPrice:" + LastPrice;
            return obj;

        }
    }


}
