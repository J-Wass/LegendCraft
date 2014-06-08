// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

//legacy autorank support for fCraft

namespace fCraft.AutoRank
{
    public sealed class fCraftCriterion : ICloneable
    {
        public Rank FromRank { get; set; }
        public Rank ToRank { get; set; }
        public ConditionSet Condition { get; set; }

        public fCraftCriterion() { }

        public fCraftCriterion([NotNull] fCraftCriterion other)
        {
            if (other == null) throw new ArgumentNullException("other");
            FromRank = other.FromRank;
            ToRank = other.ToRank;
            Condition = other.Condition;
        }

        public fCraftCriterion([NotNull] Rank fromRank, [NotNull] Rank toRank, [NotNull] ConditionSet condition)
        {
            if (fromRank == null) throw new ArgumentNullException("fromRank");
            if (toRank == null) throw new ArgumentNullException("toRank");
            if (condition == null) throw new ArgumentNullException("condition");
            FromRank = fromRank;
            ToRank = toRank;
            Condition = condition;
        }

        public fCraftCriterion([NotNull] XElement el)
        {
            if (el == null) throw new ArgumentNullException("el");

            // ReSharper disable PossibleNullReferenceException
            FromRank = Rank.Parse(el.Attribute("fromRank").Value);
            // ReSharper restore PossibleNullReferenceException
            if (FromRank == null) throw new FormatException("Could not parse \"fromRank\"");

            // ReSharper disable PossibleNullReferenceException
            ToRank = Rank.Parse(el.Attribute("toRank").Value);
            // ReSharper restore PossibleNullReferenceException
            if (ToRank == null) throw new FormatException("Could not parse \"toRank\"");

            Condition = (ConditionSet)AutoRank.fCraftConditions.Parse(el.Elements().First());
        }

        public object Clone()
        {
            return new fCraftCriterion(this);
        }

        public override string ToString()
        {
            return String.Format("Criteria( {0} from {1} to {2} )",
                                  (FromRank < ToRank ? "promote" : "demote"),
                                  FromRank.Name,
                                  ToRank.Name);
        }

        public XElement Serialize()
        {
            XElement el = new XElement("Criterion");
            el.Add(new XAttribute("fromRank", FromRank.FullName));
            el.Add(new XAttribute("toRank", ToRank.FullName));
            if (Condition != null)
            {
                el.Add(Condition.Serialize());
            }
            return el;
        }
    }
}