namespace ImageProcessor
{
    public class Logger
    {
        private Action<string> _action;

        public Logger(Action<string> action)
        {
            _action = action;
        }

        public void Log(string message)
        {
            _action(message);
        }
    }
}
