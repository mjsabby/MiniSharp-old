namespace System
{
    public class Object
    {
        public Object()
        {
        }

        public virtual String ToString()
        {
            return this.GetType().ToString();
        }

        public virtual bool Equals(Object obj)
        {
            return true;
        }

        public static bool Equals(Object objA, Object objB)
        {
            if (objA == objB)
            {
                return true;
            }

            if (objA == null || objB == null)
            {
                return false;
            }

            return objA.Equals(objB);
        }

        public static bool ReferenceEquals(Object objA, Object objB)
        {
            return objA == objB;
        }

        public virtual int GetHashCode()
        {
            return 0;
        }

        public extern Type GetType();

        ~Object()
        {
        }

        protected extern Object MemberwiseClone();
    }
}