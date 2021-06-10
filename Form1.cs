using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Data.SqlClient;
using System.IO;
using mshtml;
using System.Net;
using HtmlAgilityPack; //first addthis line--> Install-Package HtmlAgilityPack -Version 1.4.0
using IvanAkcheurov.NTextCat.Lib;     //Install-Package NTextCat.Http -Version 1.0.13
using System.Text.RegularExpressions;
using Annytab.Stemmer;

namespace information_retrieval_2
{
    public partial class Form1 : Form
    {
        SqlConnection con = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=test;Integrated Security=True");
        SqlConnection con2 = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=test;Integrated Security=True");
        DataTable dataTable = new DataTable();
        
        Queue<string> visited_urls = new Queue<string>();
        Queue<string> to_visit_urls = new Queue<string>();
       // string URL = "https://www.wikisource.org/wiki/";
        int to_visit_count = 0;
        string URL = "https://www.bbc.com/";
        int count = 0;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //string U = URL;
           
            while (count < 3030)
            {

                try
                {
                      WebRequest myWebRequest;
                      WebResponse myWebResponse;
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    myWebRequest = WebRequest.Create(URL);      // create http request with URL
                    myWebResponse = myWebRequest.GetResponse(); //get the respose of the request,the response is the entire web page


                   //  to read the response 
                    Stream streamResponse = myWebResponse.GetResponseStream();
                    StreamReader sReader = new StreamReader(streamResponse);
                    string rString = sReader.ReadToEnd();  // rString hold the entire web page,it's should be stored in database
                    streamResponse.Close();
                    sReader.Close();
                    myWebResponse.Close();
                    visited_urls.Enqueue(URL);


                    //------------------------ parse ---------------------------

                    
                        add_to_database(URL, rString);
                        if (to_visit_count+visited_urls.Count < 3030)
                        {
                            IHTMLDocument2 myDoc = new HTMLDocumentClass();
                            myDoc.write(rString);
                            IHTMLElementCollection elements = myDoc.links;
                            foreach (IHTMLElement el in elements)
                            {
                                string link = (string)el.getAttribute("href", 0);

                                
                                if (link.StartsWith("https://") || link.StartsWith("http://"))
                                {
                                    
                                    if (!visited_urls.Contains(link) && check_link(link) && !to_visit_urls.Contains(link))
                                    {
                                        try
                                        {
                                            WebRequest myWebRequest1;
                                            WebResponse myWebResponse1;
                                            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                            myWebRequest1 = WebRequest.Create(link);      // create http request with URL
                                            myWebResponse1 = myWebRequest1.GetResponse(); //get the respose of the request,the response is the entire web page


                                            // to read the response 
                                            Stream streamResponse1 = myWebResponse1.GetResponseStream();
                                            StreamReader sReader1 = new StreamReader(streamResponse1);
                                            string rString1 = sReader1.ReadToEnd();  // rString hold the entire web page,it's should be stored in database
                                            streamResponse1.Close();
                                            sReader1.Close();
                                            myWebResponse1.Close();

                                            if(check_lang(rString1))
                                            {
                                                 to_visit_urls.Enqueue(link);
                                                    to_visit_count++;
                                            }
                                           
                                        }
                                        catch { }
                                        
                                        
                                    }
                                }
                               
                            }
                            to_visit_count--;
                        }

                    
                    //------------------------------------------------------------------------------------
                    URL = to_visit_urls.Dequeue();
                }
                catch
                {
                    if (to_visit_urls.Count != 0)
                        URL = to_visit_urls.Dequeue();
                }

            }

        }
        public bool check_lang(string rString)
        {
            
            try
            {
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(rString);
                if (doc.DocumentNode.SelectSingleNode("html").Attributes["lang"].Value.Equals("en"))
                {
                    return true;
                }
            }
            catch{}

            try
            {
            var factory = new RankedLanguageIdentifierFactory();
            var identifier = factory.Load("Core14.profile.xml"); // can be an absolute or relative path. Beware of 260 chars limitation of the path length in Windows. Linux allows 4096 chars.
            var languages = identifier.Identify(rString);
            var mostCertainLanguage = languages.FirstOrDefault();
            if (mostCertainLanguage.Item1.Iso639_3.ToString() == "eng")
                 return true;
            }
            catch { }
            
            
            //

            return false;
        }

        

        public bool check_link(String URL)  
        {
            try
            {
                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(URL);
                wr.Credentials = CredentialCache.DefaultCredentials;
                // now, request the URL from the server, to check it is valid and works
                using (HttpWebResponse response = (HttpWebResponse)wr.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        // if the code execution gets here, the URL is valid and is up/works
                        return true;
                    }
                    response.Close();
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void add_to_database(string url, string content)
        {
            con.Open();
            String query = "INSERT INTO crawler_data (URL,content_data) VALUES (@URL,@content_data)";
            SqlCommand command = new SqlCommand(query, con);
            command.Parameters.Add("@URL", url);
            command.Parameters.Add("@content_data", content);
            command.ExecuteNonQuery();
            con.Close();
            count++;
        }

        public void parse(string rString)
        {
            IHTMLDocument2 myDoc = new HTMLDocumentClass();
            myDoc.write(rString);
            IHTMLElementCollection elements = myDoc.links;
            foreach (IHTMLElement el in elements)
            {
                string link = (string)el.getAttribute("href", 3);

                if (!link.StartsWith("http:") && link.StartsWith("//"))
                    link = "https:" + link;
                else if (!link.StartsWith("http:") && link.StartsWith("/"))
                    link = "https:/" + link;
                else if (!link.StartsWith("http:"))
                    link = "https://" + link;

                if (!visited_urls.Contains(link) && check_link(link) && !to_visit_urls.Contains(link))
                {
                    to_visit_urls.Enqueue(link);

                }
            }

        }

        public String fetch(string URL)
        {
            try
            {
                WebRequest myWebRequest;
                WebResponse myWebResponse;
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                myWebRequest = WebRequest.Create(URL);      // create http request with URL
                myWebResponse = myWebRequest.GetResponse(); //get the respose of the request,the response is the entire web page


                // to read the response 
                Stream streamResponse = myWebResponse.GetResponseStream();
                StreamReader sReader = new StreamReader(streamResponse);
                string rString = sReader.ReadToEnd();  // rString hold the entire web page,it's should be stored in database
                streamResponse.Close();
                sReader.Close();
                myWebResponse.Close();
                visited_urls.Enqueue(URL);
                return rString;
            }
            catch
            {
                URL = to_visit_urls.Dequeue();
                string v = fetch(URL);
                return v;
            }

        }


        private void button2_Click_1(object sender, EventArgs e)
        {

            Regex regex = new Regex("^[a-zA-Z0-9. -_?]*$");
            string select_query = "SELECT TOP (1600) [id],[content_data]FROM[test].[dbo].[crawler_data]";
            
            string[] stopwords1 = {  "a", "an", "and", "are", "as", "at", "be", "but", "by", "for",
                   "if", "in", "into", "is", "it", "no", "not", "of", "on", "or", "such", "that", "the",
                   "their", "then", "there", "these", "they", "this", "to", "was", "will", "with"};

            char[] badCharacters = { '{', '}', '[', ']', '<', '"', '>', '=', '|', '&', '\\', '.', '?', '!', ':', ';', ',', '\'', '(', ')', ' ', '/', '-', '_', '+', ' ', '*', '$', '%' };

            SqlCommand cmd = new SqlCommand(select_query, con2);
            con2.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dataTable);
            con2.Close();
            da.Dispose();
            foreach (DataRow row in dataTable.Rows)
            {
                string id = row["id"].ToString();
                string content_data = row["content_data"].ToString();
                try
                {
                    var arr = new List<string>();

                    ///parse
                    var document = new HtmlAgilityPack.HtmlDocument();
                    document.LoadHtml(content_data);

                    var htmlNodes = document.DocumentNode.SelectNodes("//body[normalize-space()]");

                    String w = "";
                    String ds = "";
                    foreach (var node in htmlNodes)
                    {

                        w = (node.InnerText);
                        ds += Regex.Replace(w, "<.*?>", String.Empty);

                    }




                    string[] we = ds.Split(badCharacters, StringSplitOptions.RemoveEmptyEntries);

                    List<string> before = new List<string>();
                    List<string> newlist = new List<string>();
                    newlist.AddRange(we);

                    List<string> lis = new List<string>();

                    List<string> stopwords = new List<string>();


                    List<string> finl = new List<string>();
                    List<string> finl_before_stemmer = new List<string>();
                    Dictionary<string, List<int>> dic = new Dictionary<string, List<int>>();
                    Dictionary<string, List<int>> dic_before_stemmer = new Dictionary<string, List<int>>();

                    stopwords.AddRange(stopwords1);
                    for (int i = 0; i < newlist.Count(); i++)
                    {

                        var r = new Regex(@"(?<=[A-Z])(?=[A-Z][a-z]) |(?<=[^A-Z])(?=[A-Z]) |(?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

                        string s1 = newlist[i];
                        string bu = r.Replace(s1, " ");
                        string[] ha = bu.Split(' ');
                        foreach (String a in ha)
                        {
                            bool isIntString = a.All(char.IsDigit);
                            if (isIntString)
                                continue;
                            bool containsInt = a.Any(char.IsDigit);
                            if (containsInt)
                                continue;
                            before.Add(a.ToLower());

                        }
                    }
                    foreach (var m in before)
                    {
                        if (!m.Contains('\n'))
                        {
                            if (m.Length != 1)
                            {
                                bool f1 = regex.IsMatch(m);
                                if (f1)
                                {
                                    Stemmer stemmer1 = new Annytab.Stemmer.EnglishStemmer();
                                    string arrayStemmer1;
                                    arrayStemmer1 = stemmer1.GetSteamWord(m);
                                    finl.Add(arrayStemmer1);
                                }

                            }

                        }
                    }
                    foreach (var m in before)
                    {
                        if (!m.Contains('\n'))
                        {
                            if (m.Length != 1)
                            {
                                bool f1 = regex.IsMatch(m);
                                if (f1)
                                {

                                    finl_before_stemmer.Add(m);
                                }

                            }

                        }
                    }
                    List<string> tokenize = new List<string>();
                    for (int i = 0; i < finl.Count(); i++)
                    {
                        if (!stopwords.Contains(finl[i]))
                        {
                            bool f1 = regex.IsMatch(finl[i]);
                            if (f1)
                                tokenize.Add(finl[i]);
                        }
                    }
                   


                    for (int g = 0; g < finl_before_stemmer.Count(); g++)
                    {
                        if (dic_before_stemmer.ContainsKey(finl_before_stemmer[g]))
                        {


                            dic_before_stemmer[finl_before_stemmer[g]].Add(g);
                            
                        }
                        else
                        {
                            dic_before_stemmer[finl[g]] = new List<int>(){g};
                        }
                        
                    }
                    for (int g = 0; g < finl.Count(); g++)
                    {
                        if (dic.ContainsKey(finl[g]))
                        {


                            dic[finl[g]].Add(g);
                            dic[finl[g]].Sort();
                        }
                        else
                        {
                            dic[finl[g]] = new List<int>() { g };
                        }

                    }
                    Stemmer stemmer = new Annytab.Stemmer.EnglishStemmer();
                    string[] arrayStemmer;

                    arrayStemmer = stemmer.GetSteamWords(dic.Keys.ToArray());

                    for(int i=0;i<dic.Count;i++)
                    {
                        string text = dic.Keys.ToArray()[i];
                        List<int> gf = dic[text];
                       
                        string positions = string.Join(",", gf.ToArray());
                        int freq = gf.Count();
                        int I_D = Int32.Parse(id);
                        

                        con.Open();
                        String insert_into_tokenz = "INSERT INTO tockenz (sring,doc_no,position,frequency) VALUES (@sring,@doc_no,@position,@frequency)";
                        SqlCommand command1 = new SqlCommand(insert_into_tokenz, con);
                        
                        command1.Parameters.Add("@sring", arrayStemmer[i]);
                        command1.Parameters.Add("@doc_no", I_D);
                        command1.Parameters.Add("@position", positions);
                        command1.Parameters.Add("@frequency", freq);
                        command1.ExecuteNonQuery();
                        con.Close();


                    }
                    for (int i = 0; i < dic_before_stemmer.Count; i++)
                    {
                        string text = dic_before_stemmer.Keys.ToArray()[i];
                       

                        con.Open();
                        String insert_into_stemmerbefore = "INSERT INTO before_stemmer (doc_id,term) VALUES (@doc_id,@term)";
                        SqlCommand command = new SqlCommand(insert_into_stemmerbefore, con);
                        int I_D = Int32.Parse(id);
                        command.Parameters.Add("@doc_id", I_D);
                        command.Parameters.Add("@term", text);
                        command.ExecuteNonQuery();
                        con.Close();

                    }


                }

                catch { }





                
            }
        }
    }
}
