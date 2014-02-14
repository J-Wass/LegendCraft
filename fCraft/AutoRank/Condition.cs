using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.AutoRank
{
    /// <summary>
    /// Simple class to hold values for autorank conditions
    /// </summary>
    public class Condition
    {
        //Values
        public string startingRank;
        public string endingRank;
        public Dictionary<string, Tuple<string, int>> conditions = new Dictionary<string, Tuple<string, int>>();

        //Constructor
        public Condition(string start, string end, string cond, string oper, string val)
        {
            startingRank = start;
            endingRank = end;
            conditions.Add(cond, new Tuple<string, int>(oper, Convert.ToInt32(val)));
        }

    }
}
