using DirectShowLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Integration;

namespace VideoUtils
{
    public class VideoPlayer
    {

        public enum PlayState
        {
            Stopped,
            Paused,
            Running,
            Init
        };

        private WindowsFormsHost owner;
        private string FileName;

        private IGraphBuilder graphBuilder = null;
        private IMediaControl mediaControl = null;
        private IMediaEventEx mediaEventEx = null;
        private IVideoWindow videoWindow = null;
        private IBasicAudio basicAudio = null;
        private IBasicVideo basicVideo = null;
        private IMediaSeeking mediaSeeking = null;
        private IMediaPosition mediaPosition = null;
        private IVideoFrameStep frameStep = null;

        private int currentVolume = 0;
        private PlayState currentState = PlayState.Stopped;
        public PlayState state
        {
            get { return currentState; }
        }

        public VideoPlayer(string file, WindowsFormsHost owner)
        {
            this.owner = owner;
            this.FileName = file;

            this.currentState = PlayState.Stopped;
            this.currentVolume = -10000; // muting the audio

            InitializePlayer();
        }

        private void InitializePlayer()
        {
            int hr = 0;

            if (FileName == string.Empty)
                return;

            this.graphBuilder = (IGraphBuilder)new FilterGraph();

            // Have the graph builder construct its the appropriate graph automatically
            hr = this.graphBuilder.RenderFile(FileName, null);
            DsError.ThrowExceptionForHR(hr);

            // QueryInterface for DirectShow interfaces
            this.mediaControl = (IMediaControl)this.graphBuilder;
            this.mediaEventEx = (IMediaEventEx)this.graphBuilder;
            this.mediaSeeking = (IMediaSeeking)this.graphBuilder;
            this.mediaPosition = (IMediaPosition)this.graphBuilder;

            // Query for video interfaces, which may not be relevant for audio files
            this.videoWindow = this.graphBuilder as IVideoWindow;
            this.basicVideo = this.graphBuilder as IBasicVideo;

            // Query for audio interfaces, which may not be relevant for video-only files
            this.basicAudio = this.graphBuilder as IBasicAudio;

            // Have the graph signal event via window callbacks for performance
            // hr = this.mediaEventEx.SetNotifyWindow(owner, WMGraphNotify, IntPtr.Zero);
            DsError.ThrowExceptionForHR(hr);

            // Setup the video window
            hr = this.videoWindow.put_Owner(owner.Handle);
            DsError.ThrowExceptionForHR(hr);

            hr = this.videoWindow.put_WindowStyle(WindowStyle.Child | WindowStyle.ClipSiblings | WindowStyle.ClipChildren);
            DsError.ThrowExceptionForHR(hr);

            GetFrameStepInterface();

            hr = this.videoWindow.SetWindowPosition(0, 0, (int)owner.Width, (int)owner.Height);

            hr = this.basicAudio.put_Volume(this.currentVolume);
        }

        public void Play()
        {
            int hr = 0;
            // Run the graph to play the media file
            hr = this.mediaControl.Run();
            DsError.ThrowExceptionForHR(hr);

            this.currentState = PlayState.Running;
        }

        //
        // Some video renderers support stepping media frame by frame with the
        // IVideoFrameStep interface.  See the interface documentation for more
        // details on frame stepping.
        //
        private bool GetFrameStepInterface()
        {
            int hr = 0;

            IVideoFrameStep frameStepTest = null;

            // Get the frame step interface, if supported
            frameStepTest = (IVideoFrameStep)this.graphBuilder;

            // Check if this decoder can step
            hr = frameStepTest.CanStep(0, null);
            if (hr == 0)
            {
                this.frameStep = frameStepTest;
                return true;
            }
            else
            {
                // BUG 1560263 found by husakm (thanks)...
                // Marshal.ReleaseComObject(frameStepTest);
                this.frameStep = null;
                return false;
            }
        }

        /*
         * Media Related methods
         */

        public void Pause()
        {
            if (this.mediaControl == null)
                return;

            // Toggle play/pause behavior
            if ((this.currentState == PlayState.Paused) || (this.currentState == PlayState.Stopped))
            {
                if (this.mediaControl.Run() >= 0)
                    this.currentState = PlayState.Running;
            }
            else
            {
                if (this.mediaControl.Pause() >= 0)
                    this.currentState = PlayState.Paused;
            }
        }

        public void SetFragment(long start_seconds, long stop_seconds)
        {
            // In Directx time is measured in 100 nanoseconds. 

            DsLong pos_start = new DsLong(start_seconds * 10000000);
            DsLong pos_stop = new DsLong(stop_seconds * 10000000);
            int hr = 0;
            // Seek to the position
            hr = this.mediaSeeking.SetPositions(pos_start, AMSeekingSeekingFlags.AbsolutePositioning, pos_stop, AMSeekingSeekingFlags.AbsolutePositioning);
        }

        public void CloseFile()
        {
            int hr = 0;

            // Stop media playback
            if (this.mediaControl != null)
                hr = this.mediaControl.Stop();

            // Clear global flags
            this.currentState = PlayState.Stopped;

            // Free DirectShow interfaces
            CloseInterfaces();

            // Clear file name to allow selection of new file with open dialog

            // No current media state
            this.currentState = PlayState.Init;

        }

        private void CloseInterfaces()
        {
            int hr = 0;

            try
            {
                lock (this)
                {
                    // Relinquish ownership (IMPORTANT!) after hiding video window
                    hr = this.videoWindow.put_Visible(OABool.False);
                    DsError.ThrowExceptionForHR(hr);
                    hr = this.videoWindow.put_Owner(IntPtr.Zero);
                    DsError.ThrowExceptionForHR(hr);

                    if (this.mediaEventEx != null)
                    {
                        hr = this.mediaEventEx.SetNotifyWindow(IntPtr.Zero, 0, IntPtr.Zero);
                        DsError.ThrowExceptionForHR(hr);
                    }

                    // Release and zero DirectShow interfaces
                    if (this.mediaEventEx != null)
                        this.mediaEventEx = null;
                    if (this.mediaSeeking != null)
                        this.mediaSeeking = null;
                    if (this.mediaPosition != null)
                        this.mediaPosition = null;
                    if (this.mediaControl != null)
                        this.mediaControl = null;
                    if (this.basicAudio != null)
                        this.basicAudio = null;
                    if (this.basicVideo != null)
                        this.basicVideo = null;
                    if (this.videoWindow != null)
                        this.videoWindow = null;
                    if (this.frameStep != null)
                        this.frameStep = null;
                    if (this.graphBuilder != null)
                        Marshal.ReleaseComObject(this.graphBuilder); this.graphBuilder = null;

                    GC.Collect();
                }
            }
            catch
            {
            }
        }

        public int SetRate(double rate)
        {
            int hr = 0;

            // If the IMediaPosition interface exists, use it to set rate
            if (this.mediaPosition != null)
            {
                hr = this.mediaPosition.put_Rate(rate);
            }

            return hr;
        }

    }
}

