using System;
using System.Web.Script.Serialization;
using QuantConnect;
using QuantConnect.Data.Market;

namespace CNGateway
{
    public class EmptyClass
    {
        static void Main(string[] args)
        {
            String strMessage = @"{""ask5"": 19.8, ""bid2_volume"": 200, ""ask1_volume"": 34168, ""ask3_volume"": 2500, ""bid1"": 19.75, ""bid3"": 19.73, ""ask2"": 19.77, ""bid2"": 19.74, ""bid4_volume"": 800, ""ask2_volume"": 11500, ""date"": ""2017-10-24"", ""volume"": 40094699.28, ""turnover"": 2054623, ""high"": 20.02, ""open"": 19.97, ""bid1_volume"": 2600, ""bid5"": 19.71, ""name"": ""\u89c6\u89c9\u4e2d\u56fd"", ""time"": ""15:25:03"", ""bid4"": 19.72, ""now"": 19.76, ""sell"": 19.76, ""bid5_volume"": 700, ""ask4_volume"": 4200, ""ask5_volume"": 11400, ""ask4"": 19.79, ""ask1"": 19.76, ""bid3_volume"": 1900, ""buy"": 19.75, ""low"": 19.3, ""close"": 19.83, ""ask3"": 19.78}";
            ParseJson(strMessage);
        }

        public static Tick ParseJson(string strMessage)
        {

            Console.WriteLine("Inside parser to parse...");
            JavaScriptSerializer json_serializer = new JavaScriptSerializer();
            //strMessage = strMessage.Replace(@"""", "'");
            SinaJson sJson = json_serializer.Deserialize<SinaJson>(strMessage);
            Console.WriteLine(sJson.name + " " + sJson.ask1);


            //create tick
            Tick newtick = new Tick()
            {

                DataType = MarketDataType.Tick,
                Time = DateTime.UtcNow,
                //Symbol = QuantConnect.Symbol.Create("JM1801", SecurityType.Equity, Market.CN),
                Symbol = QuantConnect.Symbol.Create("JM1801", SecurityType.Equity, "cn"),
                Value = strMessage.Split(',')[2].Split(':')[1].ToDecimal(),
                TickType = TickType.Quote,
                BidPrice = 111,
                AskPrice = 222,
                Quantity = 123


            };

            Console.WriteLine("Tick Created: Value:{0}", newtick.Value);

            return newtick;

        }
    }


}
