using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;

namespace VisualHG
{
    // ------------------------------------------------------------------------
    // IOleComponent OnIdle trigger
    // ------------------------------------------------------------------------
    public delegate void OnIdleEvent();

    internal class SccOnIdleEvent : IOleComponent
    {
        private uint _wComponentId;
        private IOleComponentManager _cmService;

        public event OnIdleEvent OnIdleEvent;

        public void RegisterForIdleTimeCallbacks(IOleComponentManager cmService)
        {
            _cmService = cmService;

            if (_cmService != null)
            {
                var pcrinfo = new OLECRINFO[1];
                pcrinfo[0].cbSize = (uint) Marshal.SizeOf(typeof(OLECRINFO));
                pcrinfo[0].grfcrf = (uint) _OLECRF.olecrfNeedIdleTime |
                                    (uint) _OLECRF.olecrfNeedPeriodicIdleTime;
                pcrinfo[0].grfcadvf = (uint) _OLECADVF.olecadvfModal |
                                      (uint) _OLECADVF.olecadvfRedrawOff |
                                      (uint) _OLECADVF.olecadvfWarningsOff;
                pcrinfo[0].uIdleTimeInterval = 100;

                _cmService.FRegisterComponent(this, pcrinfo, out _wComponentId);
            }
        }

        public void UnRegisterForIdleTimeCallbacks()
        {
            _cmService?.FRevokeComponent(_wComponentId);
        }

        public virtual int FContinueMessageLoop(uint uReason, IntPtr pvLoopData, MSG[] pMsgPeeked)
        {
            return 1;
        }

        /// <summary>
        ///     Idle processing trigger method
        /// </summary>
        public virtual int FDoIdle(uint grfidlef)
        {
            OnIdleEvent?.Invoke();

            return 0;
        }

        public virtual int FPreTranslateMessage(MSG[] pMsg)
        {
            return 0;
        }

        public virtual int FQueryTerminate(int fPromptUser)
        {
            return 1;
        }

        public virtual int FReserved1(uint dwReserved, uint message, IntPtr wParam, IntPtr lParam)
        {
            return 0;
        }

        public virtual IntPtr HwndGetWindow(uint dwWhich, uint dwReserved)
        {
            return IntPtr.Zero;
        }

        public virtual void OnActivationChange(IOleComponent pic, int fSameComponent, OLECRINFO[] pcrinfo,
            int fHostIsActivating, OLECHOSTINFO[] pchostinfo, uint dwReserved)
        {
            ;
        }

        public virtual void OnAppActivate(int fActive, uint dwOtherThreadId)
        {
            ;
        }

        public virtual void OnEnterState(uint uStateId, int fEnter)
        {
            ;
        }

        public virtual void OnLoseActivation()
        {
            ;
        }

        public virtual void Terminate()
        {
            ;
        }
    }
}