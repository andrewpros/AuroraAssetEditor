using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace AuroraAssetEditor.Classes {
    public class ExWebClient:WebClient {
        protected override WebRequest GetWebRequest(Uri address) {
            var request = base.GetWebRequest(address);
            HttpWebRequest webrq = null;
            if(request is HttpWebRequest) {
                webrq = request as HttpWebRequest;
                webrq.ServicePoint.Expect100Continue = false;
                webrq.KeepAlive = false;
                webrq.UserAgent = App.UserAgent;
                webrq.Proxy = null;
                webrq.ProtocolVersion = HttpVersion.Version10;

                }

            return webrq;
            }
        }
    }
