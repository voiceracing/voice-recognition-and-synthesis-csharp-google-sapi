using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Windows.Forms;

using System.Text.RegularExpressions;



/*ŹRÓDŁA:
  
  
 
 * polaczenie z serverem
 * 
 * http://stackoverflow.com/questions/8778624/trying-to-use-google-speech2text-in-c-sharp
 
 
 * znaczenie elementów GET
 *   
 * http://sebastian.germes.in/blog/2011/09/googles-speech-api/
 
 * sox
 * 
 * http://fennb.com/fast-free-speech-recognition-using-googles-in
 
 * 
 * http://stackoverflow.com/questions/8778624/trying-to-use-google-speech2text-in-c-sharp

 
 * 
 * http://developer.chrome.com/extensions/experimental.speechInput.html
 
 
 
 
 
 
 sooo easy in HTML: 
 
            <input type="text" x-webkit-speech lang="pl"/>
 * */



class Google_Speech
{

    #region Speech To Text

    public const int    DEFAULT_BIT_RATE = 8000;
    public const string DEFAULT_LANGUAGE = "en-US";

    /*ARGUMENTS DESCRIPTION:
        https://www.google.com/speech-api/v1/recognize
        The base address.
         
        xjerr=1
        Tell speech recognition server to return errors as part of the JSON response and not HTTP error codes
         
        client=chromium
        Can be anything, “chromium” is the standard one.
        
        lang=en-US
        The language to be used, so far tested with en-US, en-GB, de-DE
         
        maxresults=10
        The maximum results of hypotheses to be returned, default is 1.
         
        pfilter=0
        This is a funny one. Google (by default) censors the results, leading to “Please search for ###” (pfilter!=0) instead of “Please search for s e x” (pfilter=0).
     * 
     */

    #region Result
        public class SpeechInputResult
        {
            public string ID;
            public int status;

            /// <summary>
            /// Hypothesis
            /// </summary>
            public class Hypothesis 
            {
                /// <summary>
                /// Text of this hypothesis.
                /// </summary>
                public string utterance; 
                
                /// <summary>
                /// Confidence of the hypothesis. Rated from 0 (lowest) to 1 (highest).
                /// </summary>
                public double confidence = -1.0d;// -1 = no VALUE!

                public override string ToString()
                {
                    return "'" + utterance + "'"+ 
                            ((confidence == -1) ? "" : "@" + confidence);
                }
            }
            public List<Hypothesis> hypotheses = new List<Hypothesis>();

            public Hypothesis getBestHypothesis()
            {
                if (hypotheses.Count() <= 0)
                    return null;

                Hypothesis H = hypotheses[0];

                foreach (Hypothesis h in hypotheses)
                {
                    if (h.confidence>=H.confidence)
                    {
                        H = h;
                    }
                }

                return H;
            }

            public string json_mem = "";

            public void FromJSON(String JSON)
            {
                json_mem = JSON;

                JSON = JSON.Replace("\n","").Trim();
                Match m;

                //status
                m = new Regex("\\\"status\\\"\\:([0-9]*),", RegexOptions.IgnoreCase).Match(JSON);
                status = Int32.Parse(m.Groups[1].Value);
                //-------

                //ID
                m = new Regex("\\\"id\\\"\\:\\\"([^\\\"]*)\\\",", RegexOptions.IgnoreCase).Match(JSON);
                ID = m.Groups[1].Value;
                //-------

                //hypotheses                
                int l1 = JSON.IndexOf("hypotheses");
                    l1 = JSON.IndexOf("[",l1);
                int r1 = JSON.LastIndexOf("]");
                string JSON2 = JSON.Substring(l1, r1-l1+1);

                MatchCollection m2 = new Regex("{([^\\}]*)}", RegexOptions.IgnoreCase).Matches(JSON2);
                foreach (Match g in m2)
                {
                    string s = g.Value;
                    SpeechInputResult.Hypothesis h = new SpeechInputResult.Hypothesis();

                    m = new Regex("\\\"utterance\\\"\\:\\\"([^\\\"]*)\\\"", RegexOptions.IgnoreCase).Match(s);
                    h.utterance  = m.Groups[1].Value;

                    m = new Regex("\\\"confidence\\\"\\:([0-9\\.]*)", RegexOptions.IgnoreCase).Match(s);
                    string confidence = m.Groups[1].Value;
                    confidence = confidence.Replace(".", ",");
                    if (confidence != "")
                    {
                        h.confidence = float.Parse(confidence);
                    }

                    hypotheses.Add(h);
                }
                //-------
            }
        };
    #endregion


   
        /// <summary>
        ///
        /// </summary>
        /// <param name="path">File witch contents of a .flac encoding mono</param>
        /// <param name="BIT_RATE">16000hz or 8000hz (possibly other)</param>
        /// <param name="language">BCP-47 language code of the language to recognize. When set to 'auto' or not defined defaults to user's most preferred content language. Will use 'en-US' if not supported or invalid. Examle:http://msdn.microsoft.com/en-us/library/ms533052(v=vs.85).aspx</param>
        /// <returns></returns>
        public static SpeechInputResult ProcessFlacFile(string FlacFileName, int BIT_RATE = DEFAULT_BIT_RATE, string language = DEFAULT_LANGUAGE, uint maxresults = 1)
        {

            //połącz z serverem
            //-------------
            string client = "chromium";

            HttpWebRequest request =
                    (HttpWebRequest)HttpWebRequest.Create(
                        "https://www.google.com/speech-api/v1/recognize?"+
                        "xjerr=1"+
                        "&client="+client+
                        "&lang=" + language +
                        "&maxresults=" + maxresults+
                        "&pfilter=0");

            request.Proxy = null;//fast hack!

            ServicePointManager.ServerCertificateValidationCallback +=
                            delegate { return true; };

            request.Timeout = 6000;
            request.Method = "POST";
            request.KeepAlive = true;
            request.ContentType = "audio/x-flac; rate=" + BIT_RATE;
            request.UserAgent = client;
            //-------------

            //wczytaj plik (z dysku)
            //-------------
            byte[] data;
            FileInfo fInfo = new FileInfo(FlacFileName);
            long numBytes = fInfo.Length;

            using (FileStream fStream = new FileStream(
                                                FlacFileName,
                                                FileMode.Open,
                                                FileAccess.Read))
            {
                data = new byte[fStream.Length];
                fStream.Read(data, 0, (int)fStream.Length);
                fStream.Close();
            }
            //-------------


            //wyslij plik
            //-------------
            using (Stream wrStream = request.GetRequestStream())
                wrStream.Write(data, 0, data.Length);
            //-------------
            
            // czytaj odpowiedź
            string RET = "";
            try
            {
                var resp = ((HttpWebResponse)request.GetResponse()).GetResponseStream();

                if (resp != null)
                {
                    StreamReader sr = new StreamReader(resp);

                    RET = sr.ReadToEnd();

                    resp.Close();
                    resp.Dispose();
                }
            }
            catch (System.Exception ee)
            {
                throw ee;
            }
            //-------------

            if(RET=="")
                return null;

            SpeechInputResult SR = new SpeechInputResult();
            SR.FromJSON(RET);
            return SR;
        }

    #endregion

    #region Text To Speach

        /// <summary>
        /// tłumaczy napis(text) na dźwięk(w formacie mp3 > zapisuje do pliku)
        /// </summary>
        /// <param name="text">tekst do zamiany na dźwięk</param>
        /// <param name="OutMp3FileName">tu zapisze pobrany plik</param>
        /// <param name="lang">język [en,pl,gb]</param>
        public static 
        void Say(string text, string OutMp3FileName,string lang = "en")
        {
            //maby?
            //text = Uri.EscapeUriString(text);

            WebClient webClient = new WebClient();
            webClient.Proxy = null;
            webClient.DownloadFile("http://translate.google.com/translate_tts?"+
                                    "tl="+lang+
                                    "&q="+text,
                                    OutMp3FileName);
        }

    #endregion
}
