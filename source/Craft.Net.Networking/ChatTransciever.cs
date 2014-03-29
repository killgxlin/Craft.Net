using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Craft.Net.Common;

namespace Craft.Net.Networking
{
    //Lowercase because that's the way vanilla sends the "color" field
    //In fact, the vanilla server will not have it any other way.
    public enum ChatColor
    {
        black,
        dark_blue,
        dark_green,
        dark_aqua,
        dark_red,
        dark_purple,
        gold,
        gray,
        dark_gray,
        blue,
        green,
        aqua,
        red,
        light_purple,
        yellow,
        white,
        obfuscated,
        bold,
        strikethrough,
        underline,
        italic,
        reset
    }

    //Lowercase because that's the way vanilla sends the "clickEvent" field.
    //In fact, the vanilla server will only send the message if it's lowercase.
    public enum ChatActionType
    {
        open_url,
        open_file,
        run_command,
        suggest_command,
        none
    }
    
    //Lowercase because that's the way vanilla sends the "hoverEvent" field.
    //In fact, the vanilla server will only send the message if it's lowercase.
    public enum ChatHoverActionType
    {
        show_text,
        show_achievement,
        show_item,
        none
    }

    public class ChatMessage
    {
        #region Properties

        protected string _rawMessage;
        public string RawMessage
        {
            get { return _rawMessage; }
        }

        protected string _text;
        public string Text
        {
            get { return _text; }
            private set { _text = value; }
        }

        protected ChatColor _color;
        public ChatColor Color
        {
            get { return _color; }
            protected set { _color = value; }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Craft.Net.Networking.ChatMessage"/> is bolded.
        /// Can be overriden by submessages with a different value.
        /// </summary>
        public bool Bold { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Craft.Net.Networking.ChatMessage"/> is italicized.
        /// Can be overriden by submessages with a different value.
        /// </summary>
        public bool Italic { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Craft.Net.Networking.ChatMessage"/> is underlined.
        /// Can be overriden by submessages with a different value.
        /// </summary>
        public bool Underlined { get; protected set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Craft.Net.Networking.ChatMessage"/> is struck through.
        /// Can be overriden by submessages with a different value.
        /// </summary>
        public bool StrikeThrough { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Craft.Net.Networking.ChatMessage"/> is obfuscated.
        /// "Obfuscated" in this context means the client will display this message segment with random characters
        /// instead of the actual text.
        /// Can be overriden by submessages with a different value.
        /// </summary>
        public bool Obfuscated { get; protected set; }
        
        protected ChatActionType _action;
        public ChatActionType Action
        { 
            get { return _action; }
            protected set { _action = value; }
        }

        /// <summary>
        /// Gets or sets the chat action value.
        /// Should only be set if Action != ChatACtionType.none
        /// </summary>
        public string ChatActionValue { get; protected set; }

        protected ChatHoverActionType _hoverAction;
        public ChatHoverActionType HoverAction
        {
            get { return _hoverAction; }
            protected set { _hoverAction = value; }
        }

        public string ChatHoverActionValue { get; protected set; }

        public bool IsCommand { get; private set; }

        protected IList<ChatMessage> _subMessages;
        public IList<ChatMessage> SubMessages
        {
            get { return _subMessages; }
            protected set { _subMessages = value; }
        }

        #endregion

        #region Constructors

        public ChatMessage(string Message)
        {
            _rawMessage = Message;
            try
            {
                Init(JObject.Parse(Message));
            } catch (Newtonsoft.Json.JsonReaderException)
            {
                //If Newtonsoft.Json couldn't parse it, that means we got something that isn't a new-format chat message.
                // (Or there's a bug in the server)
                //So we're just gona set the text content and call it a day.
                _text = Message;
                if (_text.StartsWith("/"))
                    this.IsCommand = true;
            }
        }

        private ChatMessage(JObject obj)
        {
            Init(obj);
        }

        private void Init(JObject obj)
        {
            JToken temp;
            #region parsing
            if (obj.TryGetValue("text", out temp))
            {
                _text = (string)temp;
            }
            // Try to parse enumerations
            if (obj.TryGetValue("color", out temp))
            {
                if (!ChatColor.TryParse((string)temp.ToString(), out _color))
                    this.Color = ChatColor.reset;
            }
            else
            {
                this.Color = ChatColor.reset;
            }
            if (obj.TryGetValue("clickEvent", out temp))
            {
                if (!ChatActionType.TryParse((string)temp, out _action))
                    _action = ChatActionType.none;
            }
            else
            {
                _action = ChatActionType.none;
            }
            if (obj.TryGetValue("hoverEvent", out temp))
            {
                if (!ChatHoverActionType.TryParse((string)temp, out _hoverAction))
                    _hoverAction = ChatHoverActionType.none;
            }
            else
            {
                _hoverAction = ChatHoverActionType.none;
            }
            //Parse booleans
            if (obj.TryGetValue("bold", out temp))
            {
                Bold = (bool)temp;
            }
            if (obj.TryGetValue("italic", out temp))
            {
                Italic = (bool)temp;
            }
            if (obj.TryGetValue("underlined", out temp))
            {
                Underlined = (bool)temp;
            }
            if (obj.TryGetValue("strikethrough", out temp))
            {
                StrikeThrough = (bool)temp;
            }
            if (obj.TryGetValue("obfuscated", out temp))
            {
                StrikeThrough = (bool)temp;
            }
            #endregion
            #region recursion

            if (obj.TryGetValue("extra", out temp))
            {
                if (temp.Type == JTokenType.Array)
                {
                    _subMessages = new List<ChatMessage>();
                    foreach (object o in (JArray)obj["extra"])
                    {
                        if (o.GetType() == typeof(JValue))
                        {
                            Console.WriteLine(((JValue)o).Type);
                            _subMessages.Add(new ChatMessage((string)((JValue)o).Value));
                        }
                        else
                        {
                            _subMessages.Add(new ChatMessage((JObject)o));
                        }
                    }
                }
            }

            #endregion
        }

        #endregion

        #region Public methods

        public string FullText()
        {
            //TODO
            return "";
        }

        public override string ToString()
        {
            string extras = "[";
            if (_subMessages != null)
            {
                foreach (ChatMessage c in _subMessages)
                {
                    extras += c.ToString();
                    extras += ",";
                }
                extras = extras.Substring(0, extras.Length - 1); // Strip off last comma
            }
            extras += "]";
            //{}'s are added after format to avoid confusing the formating engine
            return "{" + string.Format(
                "\"text\":\"{0}\",\"bold\":{1},\"italic\":{2},\"underlined\":{3},\"strikethrough\":{4},\"obfuscated\":{5}" +
                "\"color\":{6},\"clickEvent\":{{\"action\":\"{7}\",\"value\":\"{8}\"}},\"hoverEvent\":{{\"action\":\"{9}\",\"value\":\"{10}\"}}," +
                "\"extra\":{11}",
                _text, Bold, Italic, Underlined, StrikeThrough, Obfuscated, _color,
                ((_action != ChatActionType.none) ? _action.ToString() : ""), ChatActionValue,
                ((_hoverAction != ChatHoverActionType.none) ? _hoverAction.ToString() : ""), ChatHoverActionValue, extras
                ) + "}";
        }

        #endregion
    }
}

