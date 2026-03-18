namespace Match3
{
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> available = new();
        private readonly Func<T> factory;
        private readonly Action<T>? reset;

        public ObjectPool(Func<T> factory, Action<T>? reset = null, int prewarm = 0)
        {
            this.factory = factory;
            this.reset = reset;

            for (int i = 0; i < prewarm; i++)
                available.Push(factory());
        }

        public T Get()
        {
            var item = available.Count > 0 ? available.Pop() : factory();
            return item;
        }

        public void Return(T item)
        {
            reset?.Invoke(item);
            available.Push(item);
        }
    }
}