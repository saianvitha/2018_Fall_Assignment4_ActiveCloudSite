using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IEXTrading.Infrastructure.IEXTradingHandler;
using IEXTrading.Models;
using IEXTrading.Models.ViewModel;
using IEXTrading.DataAccess;
using Newtonsoft.Json;

namespace MVCTemplate.Controllers
{
    public class HomeController : Controller
    {
        public ApplicationDbContext dbContext;

        public HomeController(ApplicationDbContext context)
        {
            dbContext = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        /****
         * The Symbols action calls the GetSymbols method that returns a list of Companies.
         * This list of Companies is passed to the Symbols View.
        ****/
        public IActionResult Symbols()
        {
            //Set ViewBag variable first
            ViewBag.dbSucessComp = 0;
            IEXHandler webHandler = new IEXHandler();
            List<Company> companies = webHandler.GetSymbols();
            companies = companies.GetRange(0, 9);
            //Save comapnies in TempData
            TempData["Companies"] = JsonConvert.SerializeObject(companies);

            return View(companies);
        }

        /****
         * The Chart action calls the GetChart method that returns 1 year's equities for the passed symbol.
         * A ViewModel CompaniesEquities containing the list of companies, prices, volumes, avg price and volume.
         * This ViewModel is passed to the Chart view.
        ****/
        public IActionResult Chart(string symbol)
        {
            //Set ViewBag variable first
            ViewBag.dbSuccessChart = 0;
            List<Equity> equities = new List<Equity>();
            if (symbol != null)
            {
                IEXHandler webHandler = new IEXHandler();
                equities = webHandler.GetChart(symbol);
                equities = equities.OrderBy(c => c.date).ToList(); //Make sure the data is in ascending order of date.
            }

            CompaniesEquities companiesEquities = getCmpnyEqtMdll(equities);

            return View(companiesEquities);
        }

        /****
         * The Refresh action calls the ClearTables method to delete records from a or all tables.
         * Count of current records for each table is passed to the Refresh View.
        ****/
        public IActionResult Refresh(string tableToDel)
        {
            ClearTables(tableToDel);
            Dictionary<string, int> tableCount = new Dictionary<string, int>();
            tableCount.Add("Companies", dbContext.Companies.Count());
            tableCount.Add("Charts", dbContext.Equities.Count());
            return View(tableCount);
        }

        /****
         * Saves the Symbols in database.
        ****/
        public IActionResult PopulateSymbols()
        {
            List<Company> companies = JsonConvert.DeserializeObject<List<Company>>(TempData["Companies"].ToString());
            foreach (Company company in companies)
            {
                //Database will give PK constraint violation error when trying to insert record with existing PK.
                //So add company only if it doesnt exist, check existence using symbol (PK)
                if (dbContext.Companies.Where(c => c.symbol.Equals(company.symbol)).Count() == 0)
                {
                    dbContext.Companies.Add(company);
                }
            }
            dbContext.SaveChanges();
            ViewBag.dbSuccessComp = 1;
            return View("Symbols", companies);
        }

        public IActionResult PopulateHealthCare()
        {
            List<Quote> companies = JsonConvert.DeserializeObject<List<Quote>>(TempData["HealthQuote"].ToString());
            foreach (Quote company in companies)
            {
                //Database will give PK constraint violation error when trying to insert record with existing PK.
                //So add company only if it doesnt exist, check existence using symbol (PK)
                if (dbContext.Quotes.Where(c => c.symbol.Equals(company.symbol)).Count() == 0)
                {
                    dbContext.Quotes.Add(company);
                }
            }
            dbContext.SaveChanges();
            ViewBag.dbSuccessComp = 1;
            return View("Top5HealthCareStocks", companies);
        }

        /****
         * Saves the equities in database.
        ****/
        public IActionResult SaveCharts(string symbol)
        {
            IEXHandler webHandler = new IEXHandler();
            List<Equity> equities = webHandler.GetChart(symbol);
            //List<Equity> equities = JsonConvert.DeserializeObject<List<Equity>>(TempData["Equities"].ToString());
            foreach (Equity equity in equities)
            {
                if (dbContext.Equities.Where(c => c.date.Equals(equity.date)).Count() == 0)
                {
                    dbContext.Equities.Add(equity);
                }
            }

            dbContext.SaveChanges();
            ViewBag.dbSuccessChart = 1;

            CompaniesEquities companiesEquities = getCmpnyEqtMdll(equities);

            return View("Chart", companiesEquities);
        }

        /****
         * Deletes the records from tables.
        ****/
        public void ClearTables(string tableToDel)
        {
            if ("all".Equals(tableToDel))
            {
                //First remove equities and then the companies
                dbContext.Equities.RemoveRange(dbContext.Equities);
                dbContext.Companies.RemoveRange(dbContext.Companies);
            }
            else if ("Companies".Equals(tableToDel))
            {
                //Remove only those that don't have Equity stored in the Equitites table
                dbContext.Companies.RemoveRange(dbContext.Companies
                                                         .Where(c => c.Equities.Count == 0)
                                                                      );
            }
            else if ("Charts".Equals(tableToDel))
            {
                dbContext.Equities.RemoveRange(dbContext.Equities);
            }
            dbContext.SaveChanges();
        }

        /****
         * Returns the ViewModel CompaniesEquities based on the data provided.
         ****/
        public CompaniesEquities getCmpnyEqtMdll(List<Equity> eqts)
        {
            List<Company> companies_list = dbContext.Companies.ToList();

            if (eqts.Count == 0)
            {
                return new CompaniesEquities(companies_list, null, "", "", "", 0, 0);
            }

            Equity current = eqts.Last();
            string dates = string.Join(",", eqts.Select(e => e.date));
            string prices = string.Join(",", eqts.Select(e => e.high));
            string volumes = string.Join(",", eqts.Select(e => e.volume / 1000000)); //Divide vol by million
            float avgprice = eqts.Average(e => e.high);
            double avgvol = eqts.Average(e => e.volume) / 1000000; //Divide volume by million
            return new CompaniesEquities(companies_list, eqts.Last(), dates, prices, volumes, avgprice, avgvol);
        }

        /**
         * This quickaction will return the top 5 stocks based on the 52-week price range strategy .
        **/
        public IActionResult Top5Stocks()
        {
            ViewBag.dbSucessComp = 0;
            IEXHandler webHandler = new IEXHandler();
            List<Company> company_list = webHandler.GetSymbols();
            company_list = company_list.Where(a => a.isEnabled && a.type != "N/A").ToList();
            List<KeyValuePair<string, Dictionary<string, Quote>>> cmpny_quotes = webHandler.GetQuotes(company_list);
            List<Company> flt_cmpny = company_list.Where(a => cmpny_quotes.Any(x => x.Key.Equals(a.symbol))).ToList();
            TempData["Quotes"] = JsonConvert.SerializeObject(cmpny_quotes);
            TempData["Companies"] = JsonConvert.SerializeObject(flt_cmpny);
            return View(flt_cmpny);
        }

        /**
         * This quickaction will return the top 5 health care stocks based on the 52-week price range strategy .
        **/
        public IActionResult Top5HealthCareStocks()
        {
            ViewBag.dbSucessComp = 0;
            IEXHandler webHandler = new IEXHandler();
            List<Quote> company_list = webHandler.GetSymbolsForHealthCare();
            List<Quote> cmpny_quotes = webHandler.GetenhancedQuotes(company_list);
            TempData["HealthQuote"] = JsonConvert.SerializeObject(cmpny_quotes);
            return View(cmpny_quotes);
        }

        /**
         * The Quotes action will pick the Top 5 stocks based on the 52-week price range strategy
     
        **/
        public IActionResult Quotes()
        {
            List<KeyValuePair<string, Dictionary<string, Quote>>> quotes = JsonConvert.DeserializeObject<List<KeyValuePair<string, Dictionary<string, Quote>>>>(TempData["Quotes"].ToString());
            List<Quote> Quote_New = new List<Quote>();
            foreach (var quoteObject in quotes)
            {
                Quote quote = quoteObject.Value.FirstOrDefault().Value;
                Quote_New.Add(quote);
            }

            return View(Quote_New);
        }

        public IActionResult ImplementationSelfReflection()
        {
            return View();
        }
    }
}
