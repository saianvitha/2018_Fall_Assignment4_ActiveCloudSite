using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using IEXTrading.Models;
using Newtonsoft.Json;

namespace IEXTrading.Infrastructure.IEXTradingHandler
{
    public class IEXHandler
    {
        static string BASE_URL = "https://api.iextrading.com/1.0/"; //This is the base URL, method specific URL is appended to this.
        HttpClient httpClient;

        public IEXHandler()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        /**
         * Calls the IEX reference API to get the list of symbols. 
        **/
        public List<Company> GetSymbols()
        {
            string IEXTrading_API_PATH = BASE_URL + "ref-data/symbols";
            string companyListjsonResponse = "";

            List<Company> full_companies_list = null;

            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage responseObj = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
            if (responseObj.IsSuccessStatusCode)
            {
                companyListjsonResponse = responseObj.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            if (!companyListjsonResponse.Equals(""))
            {
                full_companies_list = JsonConvert.DeserializeObject<List<Company>>(companyListjsonResponse);
            }
            return full_companies_list;
        }

        /**
         * Calls the IEX reference API to get the list of symbols of top stocks. 
        **/
        public List<KeyValuePair<string, Dictionary<string, Quote>>> GetQuotes(List<Company> companies_final)
        {
            int Count = 0;
            Dictionary<string, Dictionary<string, Quote>> List_Quote = new Dictionary<string, Dictionary<string, Quote>>();
            // Since we have more than 8756, and it doesn't take more than 100 at a time, running the while loop for 87 times + 56
            while (true)
            {
                //The if condition checks for count is less than Companies Count
                if (Count < companies_final.Count())
                {
                    Dictionary<string, Dictionary<string, Quote>> quotes_Name = null;
                    string companysymbols = "";
                    //The count will be skipped for every 100 companies
                    companysymbols = string.Join(",", companies_final.Select(b => b.symbol).Skip(Count).Take(100));


                    string API_PATH = BASE_URL + "stock/market/batch?symbols={0}&types=quote";
                    //string API_PATH = BASE_URL + "stock/market/collection/sector?collectionName=Health%20Care";
                    API_PATH = string.Format(API_PATH, companysymbols);
                    HttpResponseMessage output = httpClient.GetAsync(API_PATH).GetAwaiter().GetResult();
                    string Json_quote = "";
                    if (output.IsSuccessStatusCode)
                    {
                        Json_quote = output.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }

                    if (!string.IsNullOrEmpty(Json_quote))
                    {

                        quotes_Name = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Quote>>>(Json_quote);

                        quotes_Name = quotes_Name.Where(x => ((x.Value?.FirstOrDefault().Value?.companyName != "") &&
                        (x.Value?.FirstOrDefault().Value?.week52High - x.Value?.FirstOrDefault().Value?.week52Low) != 0)
                        ).ToDictionary(x => x.Key, x => x.Value);
                        Count += 100;
                        List_Quote = List_Quote.Concat(quotes_Name).ToDictionary(y => y.Key, b => b.Value);

                    }
                }
                else
                {
                    break;
                }
            }
            if (List_Quote != null)
            {
                foreach (var lq in List_Quote)
                {
                    if (lq.Value != null)
                    {
                        if (lq.Value.FirstOrDefault().Value != null)
                        {
                            var Value_quote = lq.Value.FirstOrDefault().Value;
                            lq.Value.FirstOrDefault().Value.week52PriceRange = (Value_quote.close - Value_quote.week52Low) / (Value_quote.week52High - Value_quote.week52Low);
                        }
                    }
                }
            }

            return List_Quote.OrderByDescending(y => y.Value?.FirstOrDefault().Value?.week52PriceRange).Take(5).ToList();
        }




        /**
         * Calls the IEX stock API to get 1 year's chart for the supplied symbol. 
        **/
        public List<Equity> GetChart(string symbol)
        {
            //Using the format method.
            //string IEXTrading_API_PATH = BASE_URL + "stock/{0}/batch?types=chart&range=1y";
            //IEXTrading_API_PATH = string.Format(IEXTrading_API_PATH, symbol);

            string IEXTrading_API_PATH = BASE_URL + "stock/" + symbol + "/batch?types=chart&range=1y";

            string charts = "";
            List<Equity> Equities = new List<Equity>();
            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage response = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                charts = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            if (!charts.Equals(""))
            {
                ChartRoot root = JsonConvert.DeserializeObject<ChartRoot>(charts, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                Equities = root.chart.ToList();
            }
            //make sure to add the symbol the chart
            foreach (Equity Equity in Equities)
            {
                Equity.symbol = symbol;
            }

            return Equities;
        }

        /**
         * Changes for enhancement
        **/
        public List<Quote> GetenhancedQuotes(List<Quote> companies_final)
        {
            List<Quote> List_Quote = new List<Quote>();
            companies_final = companies_final.Where(x => ((x.companyName != "") &&
                        (x.week52High - x.week52Low) != 0)
                        ).ToList();
            foreach (var company in companies_final)
            {
                company.week52PriceRange = (company.close - company.week52Low) / (company.week52High - company.week52Low);
            }

            // Since we have more than 8756, and it doesn't take more than 100 at a time, running the while loop for 87 times + 56

            return companies_final.OrderByDescending(y => y.week52PriceRange).Take(5).ToList();
        }

        public List<Quote> GetSymbolsForHealthCare()
        {
            string IEXTrading_API_PATH = BASE_URL + "/stock/market/collection/sector?collectionName=Health%20Care";
            string companyListjsonResponse = "";

            List<Quote> full_companies_list = null;

            httpClient.BaseAddress = new Uri(IEXTrading_API_PATH);
            HttpResponseMessage responseObj = httpClient.GetAsync(IEXTrading_API_PATH).GetAwaiter().GetResult();
            if (responseObj.IsSuccessStatusCode)
            {
                companyListjsonResponse = responseObj.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            if (!companyListjsonResponse.Equals(""))
            {
                full_companies_list = JsonConvert.DeserializeObject<List<Quote>>(companyListjsonResponse);
            }
            return full_companies_list;
        }
    }
}