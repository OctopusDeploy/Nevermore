using System;

namespace Nevermore
{
    public class ReferencingDocument : IEquatable<ReferencingDocument>
    {
        public string DocumentId { get; set; }
        public string DocumentName { get; set; }
        public string Relationship { get; set; }
        public IDocument Document { get; set; }

        public bool Equals(ReferencingDocument other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(DocumentId, other.DocumentId) && string.Equals(DocumentName, other.DocumentName) && Relationship == other.Relationship;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ReferencingDocument)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (DocumentId != null ? DocumentId.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (DocumentName != null ? DocumentName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Relationship != null ? Relationship.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ReferencingDocument left, ReferencingDocument right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ReferencingDocument left, ReferencingDocument right)
        {
            return !Equals(left, right);
        }
    }
}