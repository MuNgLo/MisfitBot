using System;
using System.Collections.Generic;
using System.Text;
using MisfitBot_MKII;

namespace RolesPlugin
{
    public class RolesSettings : PluginSettingsBase
    {
        public List<MarkedMessage> MarkedMessages = new List<MarkedMessage>();
        public List<TopicDefinition> Topics = new List<TopicDefinition>();
        public Dictionary<string, string> RoleTable = new Dictionary<string, string>();

        public string Roles()
        {
            string result = "";
            foreach(string key in RoleTable.Keys){
                result += $"{key}({RoleTable[key]}) ";
            }
            return result;
        }
        
        public string TopicsList(){
            string result = "";
            foreach(TopicDefinition topic in Topics){
                result += $"{topic.TopicName}({RoleList(topic)})";
            }
            return result;
        }

        public string RoleList(TopicDefinition topic)
        {
            string result = "";
            foreach(string role in topic.Roles){
                result += $"{role} ";
            }
            return result;
        }
    }// EOF CLASS
}