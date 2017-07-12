namespace Skin_Series.Core.Common
{

    using EloBuddy;
    using EloBuddy.Sandbox;

    internal static class MessageManager
    {
        private static float _lastMessageTick;

        private static string _lastMessageString;

        public static void PrintMessage(string msg, bool enabled = true)
        {
            if (enabled && _lastMessageTick + 500 > Game.Time * 1000 && _lastMessageString == msg)
            {
                return;
            }
            _lastMessageTick = Game.Time * 1000;
            _lastMessageString = msg;

            Chat.Print($"<font size=\"27\"><font color=\"{ (SandboxConfig.IsBuddy ? "#FF95CA" : "#80FFFF")}\"><b>[Skin Series]</b></font> {msg}</font>");
        }
    }
}