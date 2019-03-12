using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using System.Web;

namespace TransformFunctions
{
    public static class NLPUtilities
    {
        public static List<string> ExtractMedicalReportData(string report,ILogger log)
        {
            List<string> retVal = new List<string>();
            if (report.TrimStart().StartsWith("MSH|"))
            {
                
                string[] segs = report.Replace("\r\n", "\r").Split("\r");
                StringBuilder sb = new StringBuilder();
                sb.Append(segs[0]).Append("\r");
                for (int i=1; i < segs.Length;i++)
                {
                    if (segs[i].StartsWith("MSH"))
                    {
                        string s = ExtractReportFromHL7(sb.ToString());
                        if (!String.IsNullOrEmpty(s)) retVal.Add(s);
                        sb.Clear();
                    }
                    sb.Append(segs[i]).Append("\r");
                    
                    
                }
                if (sb.Length > 0)
                {
                    string s = ExtractReportFromHL7(sb.ToString());
                    if (!String.IsNullOrEmpty(s)) retVal.Add(s);
                }


            } else if (report.TrimStart().StartsWith("{") || report.TrimStart().StartsWith("["))
            {
                try
                {
                    if (report.TrimStart().StartsWith("{")) report = "[" + report + "]";
                    var dr = JArray.Parse(report);
                    foreach (JObject obj in dr.Children())
                    {
                        if (obj["text"] != null)
                        {
                                retVal.Add(obj["text"]["div"].GetFirstField().UnEscapeHL7());
                        }
                                            }
                }
                catch (Exception e)
                {
                    log.LogError(e, e.Message);

                }
            } else
            {
                retVal.Add(report);
            }
            return retVal;
        }
        private static string ExtractReportFromHL7(string hl7)
        {
            JObject obj = HL7ToXmlConverter.ConvertToJObject(hl7);
            string msgtype = obj["hl7message"]["MSH"]["MSH.9"].GetFirstField();
            StringBuilder builder = new StringBuilder();
            if (msgtype.ToLower().Equals("oru"))
            {
                foreach (var obx in obj["hl7message"]["OBX"])
                {
                    if (Utilities.getFirstField(obx["OBX.2"]).Equals("TX"))
                    {
                        builder.Append(Utilities.getFirstField(obx["OBX.5"]));
                    }
                }

            }
            return builder.ToString();
        }
        public static string ExtractTextUsingTIKA(byte[] byteArray,string tikaserverurl)
        {
           
            // Create a request using a URL that can receive a post. 
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(tikaserverurl);
            // Set the Method property of the request to POST.
            request.Method = "PUT";
            // Create POST data and convert it to a byte array.
            request.Headers.Add("Accept:text/plain");
            // Set the ContentType property of the WebRequest.
            //request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;
            //Console.WriteLine("Report:" + Encoding.UTF8.GetString(byteArray));
            // Get the request stream.
            using (Stream dataStream = request.GetRequestStream())
            {
                // Write the data to the request stream.
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
            // Get the response.
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            
            // Display the status.
            //Console.WriteLine(response.StatusDescription);
            // Get the stream containing content returned by the server.
            string responseFromServer = "";
            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                using (StreamReader reader = new StreamReader(dataStream))
                {
                    // Read the content.
                    responseFromServer = reader.ReadToEnd();
                }
            }
            return responseFromServer;
        }
        public static MedicalEntities ExtractMedicalEntities(CTakesRequest creq)
        {
            var medicalEntities = new MedicalEntities();
            medicalEntities.DiseaseDisorderList = new List<Term>();
            medicalEntities.MedicationMentionList = new List<Term>();
            medicalEntities.SignSymptomMentionList = new List<Term>();
            medicalEntities.AnatomicalSiteMentionList = new List<Term>();
            medicalEntities.DiseaseDisorderConceptList = new List<OntologyConcept>();
            medicalEntities.MedicationMentionConceptList = new List<OntologyConcept>();
            medicalEntities.SignSymptomMentionConceptList = new List<OntologyConcept>();
            medicalEntities.AnatomicalSiteMentionConceptList = new List<OntologyConcept>();
            medicalEntities.ConceptNameDictionary = new List<Concept>();

            try
            {

                var request = (HttpWebRequest)WebRequest.Create(creq.CTAKESUrl);

                // Take a max of X KB of text
                var subText = creq.Content.Substring(0, Math.Min(500000, creq.Content.Length));
                var postData = "q=" + HttpUtility.UrlEncode(subText, System.Text.Encoding.ASCII);
                postData += "&format=" + HttpUtility.UrlEncode(creq.Format, System.Text.Encoding.ASCII);
                postData += "&umlsuser=" + creq.UMLSUser;
                postData += "&umlspw=" + creq.UMLSPassword;
                var data = Encoding.ASCII.GetBytes(postData);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                request.Timeout = 20 * 60 * 1000;   // 20 min

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                if (responseString != "")
                {
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(responseString);
                    string parsedText = "";
                    int begin, end;
                    Guid termId;

                    foreach (var node in xml.ChildNodes[1])
                    {
                        //Sofa 
                        if (((System.Xml.XmlElement)node).LocalName == "Sofa")
                        {
                            parsedText = ((XmlElement)node).GetAttribute("sofaString");
                            medicalEntities.ParsedText = parsedText;
                        }
                        else if (((System.Xml.XmlElement)node).LocalName == "UmlsConcept")
                        {
                            int id = Convert.ToInt32(((XmlElement)node).GetAttribute("xmi:id"));
                            
                            Concept concept = new Concept()
                            {
                                ConceptName = ((XmlElement)node).GetAttribute("preferredText"),
                                CUI = ((XmlElement)node).GetAttribute("cui"),
                                ConceptId = id,
                                Code = ((XmlElement)node).GetAttribute("code"),
                                CodingSchema = ((XmlElement)node).GetAttribute("codingScheme")
                            };
                            if (!medicalEntities.ConceptNameDictionary.Contains(concept)) medicalEntities.ConceptNameDictionary.Add(concept);
                        }
                        else if (((System.Xml.XmlElement)node).LocalName == "DiseaseDisorderMention")
                        {
                            begin = Convert.ToInt32(((XmlElement)node).GetAttribute("begin"));
                            end = Convert.ToInt32(((XmlElement)node).GetAttribute("end"));

                            if (!(medicalEntities.DiseaseDisorderList.Any(t => t.term == parsedText.Substring(begin, end - begin).ToLower())))
                            {
                                termId = Guid.NewGuid();
                                medicalEntities.DiseaseDisorderList.Add(new Term
                                {
                                    termId = termId,
                                    term = parsedText.Substring(begin, end - begin).ToLower(),
                                });

                                var ontologyConceptArray = ((XmlElement)node).GetAttribute("ontologyConceptArr").ToString();
                                if (ontologyConceptArray.Length > 0)
                                {
                                    foreach (var c in ontologyConceptArray.Split(' '))
                                    {
                                        medicalEntities.DiseaseDisorderConceptList.Add(new OntologyConcept
                                        {
                                            conceptId = Guid.NewGuid(),
                                            termId = termId,
                                            ontologyConcept = c
                                        });
                                    }
                                }
                            }

                        }
                        else if (((System.Xml.XmlElement)node).LocalName == "MedicationMention")
                        {
                            begin = Convert.ToInt32(((XmlElement)node).GetAttribute("begin"));
                            end = Convert.ToInt32(((XmlElement)node).GetAttribute("end"));
                            if (!(medicalEntities.MedicationMentionList.Any(t => t.term == parsedText.Substring(begin, end - begin).ToLower())))
                            {
                                termId = Guid.NewGuid();
                                medicalEntities.MedicationMentionList.Add(new Term
                                {
                                    termId = termId,
                                    term = parsedText.Substring(begin, end - begin).ToLower()
                                });
                                var ontologyConceptArray = ((XmlElement)node).GetAttribute("ontologyConceptArr").ToString();
                                if (ontologyConceptArray.Length > 0)
                                {
                                    foreach (var c in ontologyConceptArray.Split(' '))
                                    {
                                        medicalEntities.MedicationMentionConceptList.Add(new OntologyConcept
                                        {
                                            conceptId = Guid.NewGuid(),
                                            termId = termId,
                                            ontologyConcept = c
                                        });
                                    }
                                }
                            }

                        }
                        else if (((System.Xml.XmlElement)node).LocalName == "SignSymptomMention")
                        {
                            begin = Convert.ToInt32(((XmlElement)node).GetAttribute("begin"));
                            end = Convert.ToInt32(((XmlElement)node).GetAttribute("end"));
                            if (!(medicalEntities.SignSymptomMentionList.Any(t => t.term == parsedText.Substring(begin, end - begin).ToLower())))
                            {
                                termId = Guid.NewGuid();
                                medicalEntities.SignSymptomMentionList.Add(new Term
                                {
                                    termId = termId,
                                    term = parsedText.Substring(begin, end - begin).ToLower()
                                });
                                var ontologyConceptArray = ((XmlElement)node).GetAttribute("ontologyConceptArr").ToString();
                                if (ontologyConceptArray.Length > 0)
                                {
                                    foreach (var c in ontologyConceptArray.Split(' '))
                                    {
                                        medicalEntities.SignSymptomMentionConceptList.Add(new OntologyConcept
                                        {
                                            conceptId = Guid.NewGuid(),
                                            termId = termId,
                                            ontologyConcept = c
                                        });
                                    }
                                }
                            }
                        }
                        else if (((System.Xml.XmlElement)node).LocalName == "AnatomicalSiteMention")
                        {
                            begin = Convert.ToInt32(((XmlElement)node).GetAttribute("begin"));
                            end = Convert.ToInt32(((XmlElement)node).GetAttribute("end"));
                            if (!(medicalEntities.AnatomicalSiteMentionList.Any(t => t.term == parsedText.Substring(begin, end - begin).ToLower())))
                            {
                                termId = Guid.NewGuid();
                                medicalEntities.AnatomicalSiteMentionList.Add(new Term
                                {
                                    termId = termId,
                                    term = parsedText.Substring(begin, end - begin).ToLower()
                                });
                                var ontologyConceptArray = ((XmlElement)node).GetAttribute("ontologyConceptArr").ToString();
                                if (ontologyConceptArray.Length > 0)
                                {
                                    foreach (var c in ontologyConceptArray.Split(' '))
                                    {
                                        medicalEntities.AnatomicalSiteMentionConceptList.Add(new OntologyConcept
                                        {
                                            conceptId = Guid.NewGuid(),
                                            termId = termId,
                                            ontologyConcept = c
                                        });
                                    }
                                }
                            }
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return medicalEntities;

        }

    }
}
