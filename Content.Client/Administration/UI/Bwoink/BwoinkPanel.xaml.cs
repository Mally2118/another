using Content.Shared.Administration;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.Bwoink
{
    [GenerateTypedNameReferences]
    public sealed partial class BwoinkPanel : BoxContainer
    {
        private readonly Action<string> _messageSender;

        public int Unread { get; private set; } = 0;
        public DateTime LastMessage { get; private set; } = DateTime.MinValue;
        private List<string> PeopleTyping { get; set; } = new();
        public event Action<string>? InputTextChanged;

        public BwoinkPanel(Action<string> messageSender)
        {
            RobustXamlLoader.Load(this);

            var msg = new FormattedMessage();
            msg.PushColor(Color.LightGray);
            msg.AddText(Loc.GetString("bwoink-system-messages-being-relayed-to-discord"));
            msg.Pop();
            RelayedToDiscordLabel.SetMessage(msg);

            _messageSender = messageSender;

            OnVisibilityChanged += c =>
            {
                if (c.Visible)
                    Unread = 0;
            };
            SenderLineEdit.OnTextEntered += Input_OnTextEntered;
            SenderLineEdit.OnTextChanged += Input_OnTextChanged;
            UpdateTypingIndicator();
        }

        private void Input_OnTextEntered(LineEdit.LineEditEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.Text))
                return;

            _messageSender.Invoke(args.Text);
            SenderLineEdit.Clear();
        }

        private void Input_OnTextChanged(LineEdit.LineEditEventArgs args)
        {
            InputTextChanged?.Invoke(args.Text);
        }

        public void ReceiveLine(SharedBwoinkSystem.BwoinkTextMessage message)
        {
            if (!Visible)
                Unread++;

            var formatted = new FormattedMessage(1);
            formatted.AddMarkupOrThrow($"[color=gray]{message.SentAt.ToShortTimeString()}[/color] {message.Text}");
            TextOutput.AddMessage(formatted);
            LastMessage = message.SentAt;
        }

        private void UpdateTypingIndicator()
        {
            var msg = new FormattedMessage();
            msg.PushColor(Color.LightGray);

            var text = PeopleTyping.Count == 0
                ? string.Empty
                : Loc.GetString("bwoink-system-typing-indicator",
                    ("players", string.Join(", ", PeopleTyping)),
                    ("count", PeopleTyping.Count));

            msg.AddText(text);
            msg.Pop();

            TypingIndicator.SetMessage(msg);
        }

        public void UpdatePlayerTyping(string name, bool typing)
        {
            if (typing)
            {
                if (PeopleTyping.Contains(name))
                    return;

                PeopleTyping.Add(name);
                Timer.Spawn(TimeSpan.FromSeconds(10), () =>
                {
                    if (Disposed)
                        return;

                    PeopleTyping.Remove(name);
                    UpdateTypingIndicator();
                });
            }
            else
            {
                PeopleTyping.Remove(name);
            }

            UpdateTypingIndicator();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            InputTextChanged = null;
        }
    }
}