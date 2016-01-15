using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MafiaParser
{
    class ForumPost
    {
        public ForumPost(string name, string page, string time,string post)
        {
            this.name = name;
            this.page = page;
            this.time = time;
            this.post = post;
        }

        public string name { get; set; }

        public string time { get; set; }

        public string post { get; set; }

        public string page { get; set; }
    }
}
