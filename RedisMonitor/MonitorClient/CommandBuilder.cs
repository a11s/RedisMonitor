using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorClient
{
    enum SegmentState
    {
        Start = 0,
        ProtocolHeader,
        ArgCount,
        LineHeader,
        ArgLength,
        ArgContent,
        End,
    }
    public class ProtocolPack
    {
        //StringBuilder sb = new StringBuilder();

        //bool readingLine = true;
        //bool waitCr = false;
        bool waitLf = false;
        bool waitValue = false;
        bool incompleteCommand = false;
        SegmentState _state = SegmentState.Start;
        SegmentState state
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                System.Diagnostics.Debug.WriteLine("State:" + _state.ToString());
            }
        }
        int argsCount = 0;
        int arglength = 0;
        int currentline = 0;
        int argcontentLeft = 0;
        StringBuilder sbpack = new StringBuilder();
        public List<string> allargs = new List<string>();

        public void Reset()
        {
            allargs.Clear();
            sbpack.Clear();
            waitLf = false;
            state = SegmentState.Start;
            argsCount = 0;
            arglength = 0;
            currentline = 0;
            argcontentLeft = 0;
            waitValue = false;
            incompleteCommand = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <returns>is finished?</returns>
        public bool AppendChar(char c)
        {
            if (waitValue)
            {
                if (state == SegmentState.ArgContent && waitValue && argcontentLeft > 0)
                {
                    sbpack.Append(c);
                    argcontentLeft--;
                }
                if (argcontentLeft == 0)
                {
                    waitValue = false;
                    if (incompleteCommand)
                    {
                        allargs.Add(sbpack.ToString());
                        return true;
                    }
                }

            }
            else
            {
                #region switch
                switch (c)
                {
                    case '*':
                        {
                            if (argcontentLeft == 0)
                            {
                                sbpack.Clear();
                                //readingLine = true;
                                //waitCr = false;
                                state = SegmentState.ArgCount;
                            }
                            break;
                        }
                    case '\r':
                        {
                            waitLf = true;
                            break;
                        }
                    case '\n':
                        #region LF
                        {
                            waitLf = false;
                            //waitCr = false;
                            switch (state)
                            {
                                case SegmentState.Start:
#if ENABLE1_0                                
                                //说明没有*头,属于1.0协议
                                allargs.AddRange(sbpack.ToString().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                                sbpack.Clear();
                                state = SegmentState.End;
                                return true;
#endif
                                    break;
                                case SegmentState.ProtocolHeader:
                                    break;
                                case SegmentState.ArgCount:
                                    argsCount = int.Parse(sbpack.ToString());//throw to outer function
                                    state = SegmentState.LineHeader;
                                    currentline = 0;
                                    sbpack.Clear();
                                    break;
                                case SegmentState.LineHeader:
                                    break;
                                case SegmentState.ArgLength:
                                    arglength = int.Parse(sbpack.ToString());
                                    argcontentLeft = arglength;
                                    sbpack.Clear();
                                    state = SegmentState.ArgContent;
                                    waitValue = true;
                                    break;
                                case SegmentState.ArgContent:
                                    allargs.Add(sbpack.ToString());
                                    sbpack.Clear();
                                    currentline++;
                                    if (currentline == argsCount)
                                    {
                                        //read all!
                                        state = SegmentState.End;

                                        return true;
                                    }
                                    else
                                    {
                                        state = SegmentState.LineHeader;
                                    }
                                    break;
                                case SegmentState.End:
                                    break;
                                default:
                                    break;
                            }
                            break;

                        }
                        #endregion
                    case '$':
                        #region $
                        {
                            if (state == SegmentState.LineHeader)
                            {
                                sbpack.Clear();
                                state = SegmentState.ArgLength;
                                //waitCr = true;
                            }
                            else if (state == SegmentState.Start)
                            {
                                //比如Info命令,默认只有一行,就是以$开头而不是*开头
                                sbpack.Clear();
                                state = SegmentState.ArgLength;
                                incompleteCommand = true;
                            }
                            else
                            {
                                throw new Exception("-expect Line Header");
                            }
                            break;
                        }

                        #endregion
                    default:
                        if (waitLf && c != '\n')
                        {
                            throw new Exception("-expect LF");
                        }
                        else if (state == SegmentState.LineHeader && c != '$')
                        {
                            throw new Exception("-expect Line Header");
                        }
                        else if (state == SegmentState.ArgContent)
                        {
                            sbpack.Append(c);
                            argcontentLeft--;
                            //if (argcontentLeft == 0)
                            //{
                            //    waitCr = true;
                            //}
                        }
                        else
                        {
                            //count length etc.
                            sbpack.Append(c);
                        }
                        break;
                }
                #endregion
            }

            return false;
        }
    }

    class CommandBuilder
    {
        ProtocolPack pp = new ProtocolPack();
        public bool AppendData(string s, out List<RCommand> outc)
        {
            bool hasCommand = false;
            outc = null;
            for (int i = 0; i < s.Length; i++)
            {
                var res = pp.AppendChar(s[i]);
                if (res)
                {
                    if (outc == null) { outc = new List<RCommand>(); }
                    if (!hasCommand) hasCommand = true;
                    outc.Add(new RCommand(pp.allargs));
                    pp.Reset();
                }
            }
            return hasCommand;
        }

        public void Reset()
        {
            pp.Reset();
        }
    }
}
