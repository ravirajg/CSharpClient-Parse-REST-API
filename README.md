CSharpClient-Parse-REST-API
===========================

C# wrapper for Parse.com REST API. Supports most of the functionality including "batch operations" and executing "cloud functions".
A sample project is also included in the solution which has the code samples on how to invoke the various methods on the 
ParseClient.

A big thank you to Alastair Paterson and Alden Quimby for their ideas. Borrowed some of the code from their code samples.

ParseClient is dependent on RestSharp and NewtonSoft.JSON dlls which are included as well.

How to :

Register for an account in Parse.com and get the Application Id, REST API Key, and the Master Key.

Instantiate the ParseClient using those 3 keys.






Create a New ParseObject:


public string CreateObject()
        {
            Parse.ParseObject po = new Parse.ParseObject("NewObject");
            po["name"] = "NAME";
            po["element"] = "VALUE"; ;
            Parse.ParseObject myObject = myClient.CreateObject(po);
            return myObject.objectId;
        }
        



        
Update and existing object:

        public void UpdateObject(string id)
        {
            Parse.ParseObject po = new Parse.ParseObject("NewObject");
            po["objectId"] = id;
            po["name"] = "NAME-UP";
            po["element"] = "VALUE-UP"; ;
            myClient.UpdateObject(po);
        }

Create a File in Parse:

        public void CreateFile()
        {
            Parse.ParseFile file = new Parse.ParseFile("filename"); //change
            Parse.ParseFile pf = myClient.CreateFile(file);
            Console.WriteLine(pf.Url + "<--->" + pf.Name);
            myClient.DeleteFile(file.Name);
        }


Please look at the Program.cs in the solution for other samples. 
