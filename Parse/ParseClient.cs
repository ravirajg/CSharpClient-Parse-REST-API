/*
    Copyright (c) 2014 Ravi Gunturi

    Permission is hereby granted, free of charge, to any person obtaining a copy of this
    software and associated documentation files (the "Software"), to deal in the Software
    without restriction, including without limitation the rights to use, copy, modify,
    merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to the following
    conditions:

    The above copyright notice and this permission notice shall be included in all copies
    or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
    INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
    PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
    HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
    CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using RestSharp;

namespace Parse
{
    /// <summary>
    /// A REST API based client program for maintaining data in Parse database. 
    /// </summary>
    public class ParseClient
    {
        public String ApplicationId { get; set; }
        public String ApplicationKey { get; set; }
        public String MasterKey { get; set; }
        public Int32 ConnectionTimeout { get; set; }

        private String classEndpoint = "https://api.parse.com/1/classes";
        private String fileEndpoint = "https://api.parse.com/1/files";
        private String cloudEndpoint = "https://api.parse.com/1/functions";
        private String batchEndpoint = "https://api.parse.com/1/batch";

        internal static class ParseHeaders
        {
            public const string APP_ID = "X-Parse-Application-Id";
            public const string REST_API_KEY = "X-Parse-REST-API-Key";
            public const string SESSION_TOKEN = "X-Parse-Session-Token";
            public const string MASTER_KEY = "X-Parse-Master-Key";
        }

        #region helperclasses
        //Helper classes for data returned from Parse......

        private class CreateObjectResult
        {
            public string createdAt { get; set; }
            public string objectId { get; set; }
        }

        private class CreateFileResult
        {
            public string url { get; set; }
            public string name { get; set; }
        }

        private class InternalDictionaryList
        {
            public Dictionary<String, Object>[] results { get; set; }
        }

        private class InternalDictionaryListKW
        {
            public Dictionary<String, Object>[] result { get; set; }
        }

        private class SingleObjectResult
        {
            public string result { get; set; }
        }

        #endregion
        /// <summary>
        /// ParseClient constructor
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="key"></param>
        /// <param name="mk"></param>
        /// <param name="timeout"></param>
        public ParseClient(String appId, String key, string mk, Int32 timeout = 100000)
        {
            if (String.IsNullOrEmpty(appId) || String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException();
            }

            ApplicationId = appId;
            ApplicationKey = key;
            ConnectionTimeout = timeout;
            MasterKey = mk;
        }

        /// <summary>
        /// Creates a new ParseObject
        /// </summary>
        /// <param name="PostObject">The object to be created</param>
        /// <returns>A ParseObject with the ObjectId and date of creation</returns>
        public ParseObject CreateObject(ParseObject PostObject)
        {
            if (PostObject == null)
            {
                throw new ArgumentNullException();
            }
            string responseString = SendDataToParse(PostObject.Class, PostObject);

            CreateObjectResult cor = JsonConvert.DeserializeObject<CreateObjectResult>(responseString);
            PostObject["createdAt"] = cor.createdAt;
            PostObject["objectId"] = cor.objectId;
            return PostObject;
        }

        /// <summary>
        /// Upload file to Parse
        /// </summary>
        /// <param name="parseFile">The file to upload. Its Name and Url properties will be updated upon success.</param>
        /// <returns></returns>
        public ParseFile CreateFile(ParseFile parseFile)
        {
            string responseString = UploadFileToParse(parseFile.LocalPath, parseFile.ContentType);
            CreateFileResult cor = JsonConvert.DeserializeObject<CreateFileResult>(responseString);
            parseFile.Url = cor.url;
            parseFile.Name = cor.name;
            return parseFile;
        }

        /// <summary>
        /// Delete an existing Parse File
        /// </summary>
        /// <param name="parseFile">The file to be deleted</param>
        /// <returns>returns true or false</returns>
        public bool DeleteFile(string name)
        {
            if (name != null)
            {
                return DeleteFileFromParse(name);
            }
            else
                return false;
        }

        /// <summary>
        /// Update an existing ParseObject
        /// </summary>
        /// <param name="PostObject">The object being updated</param>
        /// <returns>void</returns>
        public void UpdateObject(ParseObject PostObject)
        {
            if (PostObject == null)
            {
                throw new ArgumentNullException();
            }
            String postObjectClass = PostObject.Class;
            PostObject.Remove("Class");
            PostObject.Remove("createdAt");
            SendDataToParse(postObjectClass, PostObject, PostObject.objectId);
            PostObject.Class = postObjectClass;
        }

        /// <summary>
        /// Retrieve a Parseobject by an ID
        /// </summary>
        /// <param name="ClassName">The type of the object or class</param>
        /// <param name="ObjectId">The ObjectId of the object</param>
        /// <returns>A ParseObject with its attributes</returns>
        public ParseObject GetObject(String ClassName, String ObjectId)
        {
            if (String.IsNullOrEmpty(ClassName) || String.IsNullOrEmpty(ObjectId))
            {
                throw new ArgumentNullException();
            }

            ParseObject resultObject = new ParseObject(ClassName);

            Dictionary<String, Object> retDict = JsonConvert.DeserializeObject<Dictionary<String, Object>>(GetFromParse(classEndpoint + "/" + ClassName + "/" + ObjectId));

            foreach (var localObject in retDict)
            {
                resultObject[localObject.Key] = localObject.Value;
            }
            resultObject["Class"] = ClassName;
            return resultObject;
        }

        /// <summary>
        /// Search for objects in Parse based on attributes. 
        /// </summary>
        /// <param name="ClassName">The name of the class being queried or the type</param>
        /// <param name="Query">Various filter parameters</param>
        /// <param name="Order">The name of the attribute used to order the results. Prefacing with '-' will reverse the results.</param>
        /// <param name="Include">Names of Relationships separated by a comma eg: "type,in" . Would only work if the relationship columns are created with an "_op" type equal to either 
        /// "Add" or "AddUnique". Would not work with "op" type equal to "AddRelation"</param>
        /// <param name="Limit">The maximum number of results to be returned (Default unlimited)</param>
        /// <param name="Skip">The number of results to skip at the start (Default 0)</param>
        /// <returns>An array of ParseObjects  -- sometimes returned with their relationships as well if 'Include' parameter is passed in</returns>
        public ParseObject[] GetObjectsWithQuery(String ClassName, Object Query, string Include = null, String Order = null, Int32 Limit = 0, Int32 Skip = 0)
        {
            if (String.IsNullOrEmpty(ClassName) || Query == null)
            {
                throw new ArgumentNullException();
            }

            InternalDictionaryList dictList = JsonConvert.DeserializeObject<InternalDictionaryList>(this.GetFromParse(classEndpoint + "/" + ClassName, Query, Include, Order, Limit, Skip));

            ParseObject[] poList = new ParseObject[dictList.results.Count()];

            int i = 0;
            foreach (Dictionary<String, Object> locDict in dictList.results)
            {
                poList[i] = new ParseObject(ClassName);
                foreach (KeyValuePair<String, Object> innerDictionary in locDict)
                {
                    poList[i][innerDictionary.Key] = innerDictionary.Value;
                    poList[i]["Class"] = ClassName;
                }
                i++;
            }

            return poList;
        }

        /// <summary>
        /// Deletes an object from Parse
        /// </summary>
        /// <param name="type">The type or class of the object to be deleted</param>
        /// <param name="id">The id of the object to be deleted</param>
        public string DeleteObject(string type, string id)
        {
            RestClient client = new RestClient();
            client.BaseUrl = classEndpoint + "/" + type + "/" + id;
            RestRequest request = new RestRequest("/");
            request.Method = Method.DELETE;
            request.AddHeader("Accept", "application/json");
            request.RequestFormat = DataFormat.Json;
            request.AddHeader(ParseHeaders.APP_ID, ApplicationId);
            request.AddHeader(ParseHeaders.REST_API_KEY, ApplicationKey);
            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
                return "Success";
            else
                return "Failed";
        }

        /// <summary>
        /// This method is called by GetObjectWithId and GetObjectsWithQuery methods to retrieve one or more ParseObjects
        /// </summary>
        /// <param name="endpointUrl">The Parse end point url for classes</param>
        /// <param name="queryObject">Query / filter object that was passed in</param>
        /// <param name="includeObject">Relationships separated by comma</param>
        /// <param name="Order"></param>
        /// <param name="Limit"></param>
        /// <param name="Skip"></param>
        /// <returns>Response string from the webrequest that was executed</returns>
        private String GetFromParse(String endpointUrl, Object queryObject = null, string includeObject = null, String Order = null, Int32 Limit = 0, Int32 Skip = 0)
        {
            String finalEndpointUrl = endpointUrl + "?";
            string jsonqry = "";
            if (queryObject != null)
            {
                jsonqry += "&where=" + System.Web.HttpUtility.UrlEncodeUnicode(JsonConvert.SerializeObject(queryObject));
            }

            if (includeObject != null)
            {
                jsonqry += "&include=" + includeObject; // System.Web.HttpUtility.UrlEncodeUnicode(JsonConvert.SerializeObject(includeObject));
            }

            if (Order != null)
            {
                jsonqry += "&order=" + System.Web.HttpUtility.UrlEncodeUnicode(Order);
            }

            if (Limit != 0)
            {
                jsonqry += "&limit=" + Limit.ToString();
            }

            if (Skip != 0)
            {
                jsonqry += "&skip=" + Skip.ToString();
            }

            finalEndpointUrl = finalEndpointUrl + jsonqry.TrimStart('&');
            string finalurl = System.Web.HttpUtility.UrlDecode(finalEndpointUrl);
            RestClient client = new RestClient();
            client.BaseUrl = finalurl;
            RestRequest request = new RestRequest("/");
            request.Method = Method.GET;
            request.AddHeader("Accept", "application/json");
            request.RequestFormat = DataFormat.Json;
            request.AddHeader(ParseHeaders.APP_ID, ApplicationId);
            request.AddHeader(ParseHeaders.REST_API_KEY, ApplicationKey);
            var response = client.Execute(request);
            return response.Content;
        }

        /// <summary>
        /// This method is called by both Create / Update to create / update a ParseObject
        /// </summary>
        /// <param name="ClassName"></param>
        /// <param name="PostObject"></param>
        /// <param name="ObjectId"></param>
        /// <returns>Response string from the webrequest that was executed</returns>
        private String SendDataToParse(String ClassName, Dictionary<String, Object> PostObject, String ObjectId = null)
        {
            if (String.IsNullOrEmpty(ClassName) || PostObject == null)
            {
                throw new ArgumentNullException(string.Format("ClassName: {0} PostObject: {1}", ClassName, PostObject));
            }

            if (String.IsNullOrEmpty(ObjectId) == false)
            {
                ClassName += "/" + ObjectId;
            }

            RestClient client = new RestClient();
            client.BaseUrl = classEndpoint + "/" + ClassName;
            RestRequest request = new RestRequest("/");
            request.Method = Method.POST;
            request.AddHeader("Accept", "application/json");
            request.RequestFormat = DataFormat.Json;
            request.AddHeader(ParseHeaders.APP_ID, ApplicationId);
            request.AddHeader(ParseHeaders.REST_API_KEY, ApplicationKey);
            PostObject.Remove("Class");

            if (String.IsNullOrEmpty(ObjectId) == false)
            {
                request.Method = Method.PUT;
            }

            request.AddBody(PostObject);
            var response = client.Execute(request);

            return response.Content;

        }

        /// <summary>
        /// This method is used to upload a file to Parse
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="contentType"></param>
        /// <returns>Response string from the webrequest that was executed</returns>
        private String UploadFileToParse(string filePath, string contentType)
        {
            string fileName = Path.GetFileName(filePath);
            string dir = Path.GetDirectoryName(filePath);
            RestClient client = new RestClient();
            client.BaseUrl = fileEndpoint + "/" + fileName;
            RestRequest request = new RestRequest("/");
            request.Method = Method.POST;
            request.AddHeader("Accept", "application/json");
            request.RequestFormat = DataFormat.Json;
            request.AddHeader(ParseHeaders.APP_ID, ApplicationId);
            request.AddHeader(ParseHeaders.REST_API_KEY, ApplicationKey);
            request.AddFile(fileName, filePath);
            var response = client.Execute(request);

            return response.Content;
        }

        /// <summary>
        /// This methoed is used to delete a file from Parse
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>True or false</returns>

        private bool DeleteFileFromParse(string fileName)
        {
            RestClient client = new RestClient();
            client.BaseUrl = fileEndpoint + "/" + fileName;
            RestRequest request = new RestRequest("/");
            request.Method = Method.DELETE;
            request.AddHeader("Accept", "application/json");
            request.RequestFormat = DataFormat.Json;
            request.AddHeader(ParseHeaders.APP_ID, ApplicationId);
            request.AddHeader(ParseHeaders.REST_API_KEY, ApplicationKey);
            request.AddHeader(ParseHeaders.MASTER_KEY, MasterKey);
            var response = client.Execute(request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
                return true;
            else
                return false;
        }

        /// <summary>
        /// This method is called by all the relation operations
        /// </summary>
        /// <param name="po"></param>
        /// <param name="objs"></param>
        /// <returns></returns>
        private string RelationOperation(ParseObject po, object objs)
        {
            RestClient client = new RestClient();
            client.BaseUrl = classEndpoint + "/" + po.Class + "/" + po.objectId;
            RestRequest request = new RestRequest("/");
            request.Method = Method.PUT;
            request.AddHeader("Accept", "application/json");
            request.RequestFormat = DataFormat.Json;
            request.AddHeader(ParseHeaders.APP_ID, ApplicationId);
            request.AddHeader(ParseHeaders.REST_API_KEY, ApplicationKey);
            string s = JsonConvert.SerializeObject(objs);
            request.AddParameter("application/json", s, ParameterType.RequestBody);
            var response = client.Execute(request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
                return response.Content;
            else
                return "";
        }

        /// <summary>
        /// This method is called to add an "_op" type of "AddRelation". AddRelation --> When you would like to add a relation in such a way that it could be lazy loaded while retrieving.
        /// The pointer stored in this column is not readable but is clickable and takes you to a different table where the actual data is stored.
        /// </summary>
        /// <param name="po"></param>
        /// <param name="jo"></param>
        /// <param name="relationName"></param>
        /// <returns>Returns the timestamp at which the object was updated</returns>
        public string AddRelation(ParseObject po, Newtonsoft.Json.Linq.JObject jo, string relationName)
        {
            jo["__op"] = "AddRelation";
            Dictionary<string, object> arg = new Dictionary<string, object>();
            arg.Add(relationName, jo);
            return RelationOperation(po, arg);
        }

        /// <summary>
        /// This method is called to remove a relation that was added by calling "AddRelation"
        /// </summary>
        /// <param name="po"></param>
        /// <param name="jo"></param>
        /// <param name="relationName"></param>
        /// <returns>Returns the timestamp at which the object was updated</returns>

        public string RemoveRelation(ParseObject po, Newtonsoft.Json.Linq.JObject jo, string relationName)
        {
            jo["__op"] = "RemoveRelation";
            Dictionary<string, object> arg = new Dictionary<string, object>();
            arg.Add(relationName, jo);
            return RelationOperation(po, arg);
        }

        /// <summary>
        /// This method is called to remove a relation that was added by calling either "AddRelationLink" or "AddUniqueRelationArray"
        /// </summary>
        /// <param name="po"></param>
        /// <param name="jo"></param>
        /// <param name="relationName"></param>
        /// <returns>Returns the timestamp at which the object was updated</returns>
        public string RemoveRelationArray(ParseObject po, Newtonsoft.Json.Linq.JObject jo, string relationName)
        {
            jo["__op"] = "Remove";
            Dictionary<string, object> arg = new Dictionary<string, object>();
            arg.Add(relationName, jo);
            return RelationOperation(po, arg);
        }


        /// <summary>
        /// This method is called to add an "_op" type of "Add". You store the data in this format if you want to extract the data "pointed to" by the pointer in this column as part of the initial 
        /// retrieval itself. GetObjectswithQuery method has a parameter named "Include" and you could specify these kinds of relationships in that parameter to retrieve everything.
        /// </summary>
        /// <param name="po"></param>
        /// <param name="jo"></param>
        /// <param name="relationName"></param>
        /// <returns>Returns the timestamp at which the object was updated</returns>
        public string AddRelationLink(ParseObject po, Newtonsoft.Json.Linq.JObject jo, string relationName)
        {
            jo["__op"] = "Add";
            Dictionary<string, object> arg = new Dictionary<string, object>();
            arg.Add(relationName, jo);
            return RelationOperation(po, arg);
        }

        /// <summary>
        /// This method is called to add an "_op" type of "AddUnique". You store the data in this format if you want to extract the data "pointed to" by the pointer in this column as part of the initial 
        /// retrieval itself. GetObjectswithQuery method has a parameter named "Include" and you could specify these kinds of relationships in that parameter to retrieve everything.
        /// </summary>
        /// <param name="po"></param>
        /// <param name="jo"></param>
        /// <param name="relationName"></param>
        /// <returns>Returns the timestamp at which the object was updated</returns>

        public string AddUniqueRelationArray(ParseObject po, Newtonsoft.Json.Linq.JObject jo, string relationName)
        {
            jo["__op"] = "AddUnique";
            Dictionary<string, object> arg = new Dictionary<string, object>();
            arg.Add(relationName, jo);
            return RelationOperation(po, arg);
        }

        /// <summary>
        /// This method is called for Batchoperations. eg:an add, update, adding any relations, delete etc. Any number of functions can be chained and executed in one single request. This
        /// would hit Parse database only once and execute all operations one after another in a sequence. Currently executing a "cloud function" is not supported by Parse.
        /// </summary>
        /// <param name="objs"></param>
        /// <returns>List of objects with success or error statuses set. Each operation returns a result object.</returns>
        public List<BatchOperation> BatchOperation(object objs)
        {
            RestClient client = new RestClient();
            client.BaseUrl = batchEndpoint;
            RestRequest request = new RestRequest("/");
            request.Method = Method.POST;
            request.AddHeader("Accept", "application/json");
            request.RequestFormat = DataFormat.Json;
            request.AddHeader(ParseHeaders.APP_ID, ApplicationId);
            request.AddHeader(ParseHeaders.REST_API_KEY, ApplicationKey);
            string s = JsonConvert.SerializeObject(objs);
            request.AddParameter("application/json", s, ParameterType.RequestBody);
            var response = client.Execute(request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                List<BatchOperation> results = JsonConvert.DeserializeObject<List<BatchOperation>>(response.Content);
                return results;
            }
            else
                return new List<BatchOperation>();
        }


        /// <summary>
        /// This method is called to execute a cloud function that returns an array of ParseObjects
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ClassName"></param>
        /// <param name="parameters"></param>
        /// <returns>Returns an array of ParseObjects</returns>
        public ParseObject[] CloudFunction(string name, string ClassName, Newtonsoft.Json.Linq.JArray parameters)
        {
            RestClient client = new RestClient();
            client.BaseUrl = cloudEndpoint + "/" + name;
            RestRequest request = new RestRequest("/");
            request.Method = Method.POST;
            request.AddHeader("Accept", "application/json");
            request.RequestFormat = DataFormat.Json;
            request.AddHeader(ParseHeaders.APP_ID, ApplicationId);
            request.AddHeader(ParseHeaders.REST_API_KEY, ApplicationKey);
            foreach (Newtonsoft.Json.Linq.JObject jo in parameters)
            {
                string key = jo["name"].ToString();
                string val = jo["value"].ToString();
                request.AddParameter(key, val);
            }
            var response = client.Execute(request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                InternalDictionaryListKW dictList = JsonConvert.DeserializeObject<InternalDictionaryListKW>(response.Content);

                ParseObject[] poList = new ParseObject[dictList.result.Count()];

                int i = 0;
                foreach (Dictionary<String, Object> locDict in dictList.result)
                {
                    poList[i] = new ParseObject(ClassName);
                    foreach (KeyValuePair<String, Object> innerDictionary in locDict)
                    {
                        poList[i][innerDictionary.Key] = innerDictionary.Value;
                        poList[i]["Class"] = ClassName;
                    }
                    i++;
                }

                return poList;
            }
            else
            {
                return new ParseObject[]{};
            }
        }

        /// <summary>
        /// This method is called to execute a cloud function that returns a string result
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ClassName"></param>
        /// <param name="parameters"></param>
        /// <returns>string</returns>

        public string CloudFunctionWithStringResult(string name, string ClassName, Newtonsoft.Json.Linq.JArray parameters)
        {
            RestClient client = new RestClient();
            client.BaseUrl = cloudEndpoint + "/" + name;
            RestRequest request = new RestRequest("/");
            request.Method = Method.POST;
            request.AddHeader("Accept", "application/json");
            request.RequestFormat = DataFormat.Json;
            request.AddHeader(ParseHeaders.APP_ID, ApplicationId);
            request.AddHeader(ParseHeaders.REST_API_KEY, ApplicationKey);
            //request.AddBody(parameters);//new {kws=parameters});
            foreach (Newtonsoft.Json.Linq.JObject jo in parameters)
            {
                string key = jo["name"].ToString();
                string val = jo["value"].ToString();
                request.AddParameter(key, val);
            }
            var response = client.Execute(request);
            if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
                SingleObjectResult resultobject = JsonConvert.DeserializeObject<SingleObjectResult>(response.Content);
                return resultobject.result;
            }
            else
                return "";

        }

        

        //not used right now!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        public Parse.ParseObject GetObjectBasedOnColumnEquals(string ClassName, Newtonsoft.Json.Linq.JArray parameters)
        {
            //not used right now!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            ParseObject resultObject = new ParseObject("");
            RestClient client = new RestClient();
            client.BaseUrl = classEndpoint + "/" + ClassName + "?Where=";
            RestRequest request = new RestRequest("/");
            request.Method = Method.GET;
            request.AddHeader("Accept", "application/json");
            request.RequestFormat = DataFormat.Json;
            request.AddHeader(ParseHeaders.APP_ID, ApplicationId);
            request.AddHeader(ParseHeaders.REST_API_KEY, ApplicationKey);
            //foreach (Newtonsoft.Json.Linq.JObject jo in parameters)
            //{
            //    string key = jo["name"].ToString();
            //    string val = jo["value"].ToString();
            //    request.AddUrlSegment(key, val);
            //}
            //var response = client.Execute(request);
            //Dictionary<String, Object> retDict = JsonConvert.DeserializeObject<Dictionary<String, Object>>(response.Content);

            //foreach (var localObject in retDict)
            //{
            //    resultObject[localObject.Key] = localObject.Value;
            //}


            Dictionary<String, Object> retDict = JsonConvert.DeserializeObject<Dictionary<String, Object>>(GetFromParse(classEndpoint + "/" + ClassName + "/" + parameters));

            foreach (var localObject in retDict)
            {
                resultObject[localObject.Key] = localObject.Value;
            }
            resultObject["Class"] = ClassName;
            return resultObject;


        }

    }
}
