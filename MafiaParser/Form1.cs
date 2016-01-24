using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Threading;
using System.Configuration;

namespace MafiaParser
{
    public partial class Form1 : Form
    {
        Dictionary<int, string> dictionary = new Dictionary<int, string>()
        {
            {1,"orange"},
            {2,"green"},
            {3,"crimson"},
            {4,"DarkMagenta"},
            {5,"DeepSkyBlue"},
            {6,"Indigo"},
            {7,"HotPink"},
            {8,"RebeccaPurple"},
            {9,"SteelBlue"},
            {10,"Gold"},
            {11,"Tan"},
            {12,"Teal"},
            {13,"Violet"},
            {14,"DarkGoldenRod"},
            {15,"DarkSalmon"}

        };
        List<ForumPost> posts = new List<ForumPost>();
        DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory());
        string cookie = "";
        bool error = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            panel1.Visible = true;
            label5.Visible = true;
            pictureBox1.Visible = true;

            button3.Enabled = false;
            comboBox1.Enabled = false;
            textBox1.Enabled = false;
            checkBox1.Enabled = false;
            checkBox2.Enabled = false;

            comboBox1.Items.Clear();
            comboBox1.Items.Add("All");

            Thread thread = new Thread(this.loadPosts);
            thread.IsBackground = true;
            thread.Start();
        }

        private void loadPosts()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://app.roll20.net/sessions/create");
                request.CookieContainer = new CookieContainer();
                String data = "email=flame9040@gmail.com&password=NOTMYPASSWORD";
                byte[] byteArray = Encoding.UTF8.GetBytes(data);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;


                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                WebResponse response = request.GetResponse();

                Console.WriteLine(((HttpWebResponse)response).StatusDescription);

                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);

                //string responseFromServer = reader.ReadToEnd();
                // Console.WriteLine(responseFromServer);
                CookieCollection cookieJar = ((HttpWebResponse)response).Cookies;
                foreach (Cookie c in cookieJar)
                {
                    if (c.Name == "rack.session")
                    {
                        cookie = c.Value;
                    }
                }


                reader.Close();
                dataStream.Close();
                response.Close();

                error = false;
                posts.Clear();
                int count = 0;
                //SHDocVw.InternetExplorer IE = new SHDocVw.InternetExplorer();
                //IE.Visible = false;
                for (int i = (int)Math.Floor(minPage.Value); i <= (int)Math.Floor(maxPage.Value); i++)
                {
                    HttpWebRequest forumPageRequest = (HttpWebRequest)WebRequest.Create(File.ReadAllText("MafiaUrlConfig") + "?pagenum=" + i);
                    forumPageRequest.Method = "GET";
                    forumPageRequest.Headers.Add("cookie", "rack.session="+cookie);
                    WebResponse forumResponse = forumPageRequest.GetResponse();
                    Stream forumStream = forumResponse.GetResponseStream();
                    StreamReader forumReader = new StreamReader(forumStream);
                    string content = forumReader.ReadToEnd();


                    File.WriteAllText("temporaryHold.html", content);

                    // load snippet
                    HtmlAgilityPack.HtmlDocument htmlSnippet = new HtmlAgilityPack.HtmlDocument();
                    htmlSnippet = LoadHtmlSnippetFromFile();

                    // extract Posts
                    if (ExtractAllPosts(htmlSnippet, i.ToString()) != null)
                    {
                        posts.AddRange(ExtractAllPosts(htmlSnippet, i.ToString()));
                    }
                    else
                    {
                        error = true;
                        posts.Clear();
                        break;
                    }

                }
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("Please click on the New Game button in the bottom left of the page to add your game");
            }

            try
            {
                using (StreamWriter sw = File.CreateText("All.html"))
                {
                    sw.WriteLine("<head>");
                    sw.WriteLine("<meta charset='utf-8'>");
                    sw.WriteLine("</head>");
                    sw.WriteLine("<body>");
                    if (!error)
                    {
                        foreach (ForumPost f in posts)
                        {
                            AddToComboBox(f.name);
                            sw.WriteLine("<table style='width:100%'><tr><td><h3><font color='" + dictionary[comboBox1.Items.IndexOf(f.name)] + "'>" + f.name + "</font></h3></td>" + "<td align='right'><h4> Page: " + f.page + "</h4></td></tr>");
                            sw.WriteLine("<h4>" + f.time + "</h4>");
                            sw.WriteLine("<p>" + f.post + "</p>");
                            sw.WriteLine();
                            sw.WriteLine("<hr>");

                        }
                        sw.WriteLine("</body>");
                    }
                    else
                    {
                        sw.WriteLine("<h1>ERROR</h1>");
                        sw.WriteLine("</body>");
                    }
                }
                NavigateWebBrowserThread(di.ToString() + "\\All.html");
                MovePostsToMainThread(posts);
                SetComboBoxToIndexThread(0);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        #region Threading crap
        delegate void AddToComboBoxCallback(string name);
        delegate void MovePostsToMainThreadCallback(List<ForumPost> threadedPosts);
        delegate void NavigateWebBrowserThreadCallback(string url);
        delegate void SetComboBoxToIndexThreadCallback(int index);
        private void AddToComboBox(string name)
        {
            if (this.comboBox1.InvokeRequired)
            {
                AddToComboBoxCallback d = new AddToComboBoxCallback(AddToComboBox);
                this.Invoke(d, new object[] { name });
            }
            else
            {
                if (!comboBox1.Items.Contains(name))
                {
                    comboBox1.Items.Add(name);
                }
            }
        }

        private void MovePostsToMainThread(List<ForumPost> threadedPosts)
        {
            if (this.comboBox1.InvokeRequired)
            {
                MovePostsToMainThreadCallback d = new MovePostsToMainThreadCallback(MovePostsToMainThread);
                this.Invoke(d, new object[] { threadedPosts });
            }
            else
            {
                posts = threadedPosts;
            }
        }

        private void NavigateWebBrowserThread(string url)
        {
            if (this.comboBox1.InvokeRequired)
            {
                NavigateWebBrowserThreadCallback d = new NavigateWebBrowserThreadCallback(NavigateWebBrowserThread);
                this.Invoke(d, new object[] { url });
            }
            else
            {
                webBrowser1.Navigate(di.ToString() + "\\All.html");
            }
        }
        private void SetComboBoxToIndexThread(int index)
        {
            if (this.comboBox1.InvokeRequired)
            {
                SetComboBoxToIndexThreadCallback d = new SetComboBoxToIndexThreadCallback(SetComboBoxToIndexThread);
                this.Invoke(d, new object[] { index });
            }
            else
            {
                comboBox1.SelectedIndex = index;

            }
        }
        #endregion

        private HtmlAgilityPack.HtmlDocument LoadHtmlSnippetFromFile()
        {
            try
            {
                TextReader reader = File.OpenText("temporaryHold.html");

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.Load(reader);

                reader.Close();

                return doc;
            }
            catch (Exception e)
            {
                MessageBox.Show("Something went very VERY wrong. You should never see this error. You will not be able to fix this. Here is the error:\n" + e.Message);
                return null;
            }
        }
        private List<ForumPost> ExtractAllPosts(HtmlAgilityPack.HtmlDocument htmlSnippet, string page)
        {
            List<ForumPost> posts = new List<ForumPost>();
            HtmlNode htmlNode = null;
            try
            {
                htmlNode = htmlSnippet.DocumentNode.SelectNodes("//div[@class='container']").FirstOrDefault();
            }
            catch (Exception)
            {
                MessageBox.Show("Could not connect to forum post. Make sure that you are logged into roll20 on Internet Explorer, and that you have the correct url. \n\nNote: This can also happen if you are not connected to the internet.");
                return null;
            }
            try
            {
                foreach (var link in htmlNode.SelectNodes(".//div[@class='posts']").FirstOrDefault().SelectNodes(".//div[@class='post']"))
                {

                    string name = link.SelectNodes(".//div[@class='name']").FirstOrDefault().ChildNodes[1].InnerText.Replace("\n", "").Trim();
                    string time = link.SelectNodes(".//div[@class='timecontainer']").FirstOrDefault().ChildNodes[1].InnerText.Trim();
                    string post = link.SelectNodes(".//div[@class='postcontent redactor_editor']").FirstOrDefault().InnerHtml.Replace("&nbsp;", "<br>").Replace("<blockquote>", "<blockquote><i><font color='grey'>").Replace("</blockquote>", "</font></i></blockquote>").Trim();
                    posts.Add(new ForumPost(name, page, time, post));
                }
            }
            catch (Exception)
            {
                return new List<ForumPost>();
            }
            return posts;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFilter();
            if (webBrowser1.Document.Body != null)
                webBrowser1.Document.Body.Focus();
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateFilter();
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            UpdateFilter();
        }
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            UpdateFilter();
        }
        private void UpdateFilter()
        {
            try
            {
                using (StreamWriter sw = File.CreateText(comboBox1.Text + ".html"))
                {
                    sw.WriteLine("<head>");
                    sw.WriteLine("<meta charset='utf-8'>");
                    sw.WriteLine("</head>");
                    sw.WriteLine("<body>");
                    if (!error)
                    {
                        foreach (ForumPost f in posts)
                        {
                            if ((comboBox1.Text == "All" || f.name == comboBox1.Text) &&
                                ((f.post.ToLower().Contains(textBox1.Text.ToLower()) && !checkBox1.Checked) || (Regex.IsMatch(f.post, @"\b" + Regex.Escape(textBox1.Text) + "\\b", RegexOptions.IgnoreCase) && checkBox1.Checked)) &&
                                (!checkBox2.Checked || checkBox2.Checked && Regex.IsMatch(f.post, Regex.Escape("<strong>#vote_"), RegexOptions.IgnoreCase) || checkBox2.Checked && Regex.IsMatch(f.post, Regex.Escape("<strong>#unvote_"), RegexOptions.IgnoreCase) || checkBox2.Checked && Regex.IsMatch(f.post, Regex.Escape("<strong>vote_"), RegexOptions.IgnoreCase) || checkBox2.Checked && Regex.IsMatch(f.post, Regex.Escape("<strong>unvote_"), RegexOptions.IgnoreCase)))
                            {

                                sw.WriteLine("<table style='width:100%'><tr><td><h3><font color='" + dictionary[comboBox1.Items.IndexOf(f.name)] + "'>" + f.name + "</font></h3></td>" + "<td align='right'><h4> Page: " + f.page + "</h4></td></tr>");
                                sw.WriteLine("<tr><td><h4>" + f.time + "</h4></td></tr></table>");
                                if (textBox1.Text != "")
                                {
                                    string highlightedPost = "";
                                    if (checkBox1.Checked)
                                    {
                                        highlightedPost = Regex.Replace(f.post, @"\b" + Regex.Escape(textBox1.Text) + "\\b", "<font color='red'>" + textBox1.Text.ToUpper() + "</font>", RegexOptions.IgnoreCase);
                                    }
                                    else
                                    {
                                        highlightedPost = Regex.Replace(f.post, textBox1.Text, "<font color='red'>" + textBox1.Text.ToUpper() + "</font>", RegexOptions.IgnoreCase);
                                    }
                                    sw.WriteLine("<p>" + highlightedPost + "</p>");
                                }
                                else
                                {
                                    sw.WriteLine("<p>" + f.post + "</p>");
                                }
                                sw.WriteLine();
                                sw.WriteLine("<hr>");
                                sw.WriteLine();
                            }

                        }
                    }
                    else
                    {
                        sw.WriteLine("<h1 align='center'>ERROR</h1>");
                        sw.WriteLine("<h4 align='center'>Please make sure that you are logged into Roll20 on Internet Explorer and that the URL is correct for the forum post.</h4>");
                    }
                    sw.WriteLine("</body>");
                }
                webBrowser1.Navigate(di.ToString() + "\\" + comboBox1.Text + ".html");
            }
            catch (Exception e)
            {
                MessageBox.Show("Something went wrong while we were updating your filters! Here is the error:\n" + e.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(di.ToString() + "\\" + comboBox1.Text + ".html");
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            button1.Visible = true;
            button2.Visible = true;
            button3.Visible = false;
            panel1.Visible = false;
            label5.Visible = false;
            pictureBox1.Visible = false;

            comboBox1.Enabled = true;
            textBox1.Enabled = true;
            checkBox1.Enabled = true;
            checkBox2.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            UrlSettings form = new UrlSettings();
            form.Show();
        }
    }
}
