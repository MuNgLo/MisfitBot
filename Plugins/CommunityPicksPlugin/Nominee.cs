namespace CommunityPicksPlugin{
    
    internal struct Nominee{
        public readonly int ID;
        public readonly string Name;
        public readonly ulong SubmitterDID; // Always discord ID
        public ulong messageID;
        public int Timestamp;
        public ulong LastModActionByDID;
        public bool InUse;
        internal Nominee(int id, string name, ulong Submitter){
            ID = id; Name = name; SubmitterDID = Submitter;
            messageID = 0; Timestamp = -1; LastModActionByDID = 0; InUse = false;
        }
    }
}