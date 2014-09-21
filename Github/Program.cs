using Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Github
{
    class Program
    {
        ParseClient myClient = null;
        Program()
        {
            myClient = new Parse.ParseClient("APPID", "RESTAPIKEY", "MASTERKEY"); //change
        }

        public string CreateObject()
        {
            Parse.ParseObject po = new Parse.ParseObject("NewObject");
            po["name"] = "NAME";
            po["element"] = "VALUE"; ;
            Parse.ParseObject myObject = myClient.CreateObject(po);
            return myObject.objectId;
        }

        public void UpdateObject(string id)
        {
            Parse.ParseObject po = new Parse.ParseObject("NewObject");
            po["objectId"] = id;
            po["name"] = "NAME-UP";
            po["element"] = "VALUE-UP"; ;
            myClient.UpdateObject(po);
        }

        public void CreateFile()
        {
            Parse.ParseFile file = new Parse.ParseFile("filename"); //change
            Parse.ParseFile pf = myClient.CreateFile(file);
            Console.WriteLine(pf.Url + "<--->" + pf.Name);
            myClient.DeleteFile(file.Name);
        }


        public void GetParseObjectById(string type, string id)
        {
            ParseObject po = myClient.GetObject(type, id);
            Console.WriteLine(po["name"].ToString()); 
        }

        public void GetParseObjectsWithQry(string type, string relationshiparr)
        {
            Newtonsoft.Json.Linq.JObject ijq = new Newtonsoft.Json.Linq.JObject();
            ijq["element"] = "VALUE-UP";
            ijq["name"] = "NAME-UP";
            ParseObject[] list = myClient.GetObjectsWithQuery(type, ijq, relationshiparr);
            Console.WriteLine(list[0]["name"].ToString());
        }


        public void AddRelationLink()
        {
            ParseObject po = myClient.GetObject("NewObject", "eJQRHqGGtZ");
            JObject ido = new JObject();
            ido["__type"] = "Pointer";
            ido["className"] = "IDProv";
            ido["objectId"] = "U664u5HwsY";
            JArray ja = new JArray();
            ja.Add(ido);
            JObject wo = new JObject();
            wo["objects"] = ja;

            //for multiple objects...
            /*JArray jacts = new JArray();
            foreach (pointer p in loc.offers)
            {
                //Console.WriteLine(p.className + " " + p.objectId);
                JObject iact = new JObject();
                iact["__type"] = "Pointer";
                iact["className"] = "Activity";
                string actid = p.objectId;
                iact["objectId"] = activitymap[actid];
                jacts.Add(iact);
            }
            JObject wao = new JObject();
            wao["objects"] = jacts;
            */
            Console.WriteLine(myClient.AddRelationLink(po, wo, "type"));
        }

        public void AddRelation()
        {
            ParseObject po = myClient.GetObject("NewObject", "eJQRHqGGtZ");
            JObject ido = new JObject();
            ido["__type"] = "Pointer";
            ido["className"] = "IDProv";
            ido["objectId"] = "U664u5HwsY";
            JArray ja = new JArray();
            ja.Add(ido);
            JObject wo = new JObject();
            wo["objects"] = ja;

            //for multiple objects...look at the method above
            Console.WriteLine(myClient.AddRelation(po, wo, "reltest"));
        }

        public void AddUniqueRelationArray()
        {
            ParseObject po = myClient.GetObject("NewObject", "eJQRHqGGtZ");
            JObject ido = new JObject();
            ido["__type"] = "Pointer";
            ido["className"] = "IDProv";
            ido["objectId"] = "U664u5HwsY";
            JArray ja = new JArray();
            ja.Add(ido);
            JObject wo = new JObject();
            wo["objects"] = ja;

            Console.WriteLine(myClient.AddUniqueRelationArray(po, wo, "reluqtest"));
        }

        public void RemoveRelationArray()
        {
            ParseObject po = myClient.GetObject("NewObject", "eJQRHqGGtZ");
            List<Parse.ParseObject> list = new List<Parse.ParseObject>();
            ParseObject ao = myClient.GetObject("IDProv", "U664u5HwsY");
            list.Add(ao);
            Newtonsoft.Json.Linq.JObject jo = new Newtonsoft.Json.Linq.JObject();
            jo["objects"] = Newtonsoft.Json.Linq.JToken.FromObject(list.Select(x => new Parse.ParsePointer(x)).ToList());
            Console.WriteLine("Removing Relation .." + myClient.RemoveRelationArray(po, jo, "type"));
        }


        public void RemoveRelation()
        {
            ParseObject po = myClient.GetObject("NewObject", "eJQRHqGGtZ");
            List<Parse.ParseObject> list = new List<Parse.ParseObject>();
            ParseObject ao = myClient.GetObject("IDProv", "U664u5HwsY");
            list.Add(ao);
            Newtonsoft.Json.Linq.JObject jo = new Newtonsoft.Json.Linq.JObject();
            jo["objects"] = Newtonsoft.Json.Linq.JToken.FromObject(list.Select(x => new Parse.ParsePointer(x)).ToList());
            Console.WriteLine("Removing Relation .." + myClient.RemoveRelation(po, jo, "reltest"));
        }


        public void BatchOperation()
        {
            JArray br = new JArray();

            JObject cr = new JObject();
            cr["method"] = "PUT";
            cr["path"] = "/1/classes/NewObject/eJQRHqGGtZ";
            JObject icr = new JObject();
            icr["firstcol"] = "First Add from Batch";
            cr["body"] = icr;

            JObject lr = new JObject();
            lr["method"] = "PUT";
            lr["path"] = "/1/classes/NewObject/eJQRHqGGtZ";
            JObject ilr = new JObject();
            ilr["secondcol"] = "Second Add from Batch";
            lr["body"] = ilr;

            br.Add(cr);
            br.Add(lr);



            //if you want to add a Relationship too..as part of the batch operation..her's how you would do it
            //Assuming "NewObject2" is a diffent class
            /*JArray ios = new JArray();
            ParseObject[] objs = myClient.GetObjectsWithQuery("NewObject2", new { });
            foreach (ParseObject obj in objs)
            {
                JObject ipjo = new JObject();
                ipjo["__type"] = "Pointer";
                ipjo["className"] = obj.Class;
                ipjo["objectId"] = obj.objectId;
                ios.Add(ipjo);
            }
            JObject flayer = new JObject();
            flayer["objects"] = ios;
            flayer["__op"] = "AddRelation";

            JObject col = new JObject();
            col["tops"] = flayer;

            JObject pointerreq = new JObject();
            pointerreq["method"] = "PUT";
            pointerreq["path"] = "/1/classes/NewObject/eJQRHqGGtZ";
            pointerreq["body"] = col;

            br.Add(pointerreq);*/

            
            JObject reqo = new JObject();
            reqo["requests"] = br;

            myClient.BatchOperation(reqo);

            ///!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!////////////////////////
            //Batch operation doesnt support cloud functions..when I last looked at this function..but if its now supported the below code should work
            //error --> {"code":107,"error":"Method 'POST' to '/1/functions/testfun' not supported in batch operations."}
            //JObject cloud = new JObject();
            //cloud["method"] = "POST";
            //cloud["path"] = "/1/functions/testfun";
            //JObject bodyobj = new JObject();
            //bodyobj["locid"] = "UhS4jF39hr";
            //cloud["body"] = bodyobj;
            //br.Add(cloud);
        }

        public void DeleteObject()
        {
            myClient.DeleteObject("Toppings", "2WmZYG0r2A");
        }

        static void Main(string[] args)
        {
            Program prg = new Program();
            //string id = prg.CreateObject();
            //prg.UpdateObject(id);
            //prg.CreateFile();
            //prg.GetParseObjectById("NewObject", "eJQRHqGGtZ");
            //prg.GetParseObjectsWithQry("NewObject","type,reluqtest");
            //prg.AddRelationLink();
            //prg.AddUniqueRelationArray();
            //prg.RemoveRelationArray();
            //prg.AddRelation();
            //prg.RemoveRelation();
            //prg.BatchOperation();
            prg.DeleteObject();
        }
    }
}
