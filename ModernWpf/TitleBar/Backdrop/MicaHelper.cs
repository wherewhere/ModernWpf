using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace ModernWpf.Controls.Primitives
{
    public enum BackdropType
    {
        None = 1,
        Mica = 2,
        Acrylic = 3,
        Tabbed = 4
    }

    public static class MicaHelper
    {
        /// <summary>
        /// Checks if the current <see cref="Window"/> supports selected <see cref="BackdropType"/>.
        /// </summary>
        /// <param name="type">Background type to check.</param>
        /// <returns><see langword="true"/> if <see cref="BackdropType"/> is supported.</returns>
        public static bool IsSupported(this BackdropType type)
        {
            if (!OSVersionHelper.IsWindowsNT) { return false; }

            return type switch
            {
                BackdropType.None => OSVersionHelper.OSVersion >= new Version(10, 0, 21996), // Insider with new API                
                BackdropType.Tabbed => OSVersionHelper.OSVersion >= new Version(10, 0, 22523),
                BackdropType.Mica => OSVersionHelper.OSVersion >= new Version(10, 0, 21996),
                BackdropType.Acrylic => OSVersionHelper.OSVersion >= new Version(10, 0, 22523),
                _ => false
            };
        }

        /// <summary>
        /// Applies selected background effect to <see cref="Window"/> when is rendered.
        /// </summary>
        /// <param name="window">Window to apply effect.</param>
        /// <param name="type">Background type.</param>
        /// <param name="force">Skip the compatibility check.</param>
        public static bool Apply(Window window, BackdropType type, bool force = false)
        {
            if (!force && (!IsSupported(type))) { return false; }

            var windowHandle = new WindowInteropHelper(window).EnsureHandle();

            if (windowHandle == IntPtr.Zero) { return false; }

            Apply(windowHandle, type);

            return true;
        }

        /// <summary>
        /// Applies selected background effect to <c>hWnd</c> by it's pointer.
        /// </summary>
        /// <param name="handle">Pointer to the window handle.</param>
        /// <param name="type">Background type.</param>
        /// <param name="force">Skip the compatibility check.</param>
        public static bool Apply(IntPtr handle, BackdropType type, bool force = false)
        {
            if (!force && (!IsSupported(type))) { return false; }

            if (handle == IntPtr.Zero) { return false; }

            return type switch
            {
                BackdropType.None => TryApplyNone(handle),
                BackdropType.Mica => TryApplyMica(handle),
                BackdropType.Acrylic => TryApplyAcrylic(handle),
                BackdropType.Tabbed => TryApplyTabbed(handle),
                _ => false
            };
        }

        /// <summary>
        /// Tries to remove background effects if they have been applied to the <see cref="Window"/>.
        /// </summary>
        /// <param name="window">The window from which the effect should be removed.</param>
        public static void Remove(Window window)
        {
            var windowHandle = new WindowInteropHelper(window).EnsureHandle();

            if (windowHandle == IntPtr.Zero) return;

            Remove(windowHandle);
        }

        /// <summary>
        /// Tries to remove all effects if they have been applied to the <c>hWnd</c>.
        /// </summary>
        /// <param name="handle">Pointer to the window handle.</param>
        public static unsafe void Remove(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return;

            void* pvAttribute = (void*)(int)PvAttribute.Disable;
            void* backdropPvAttribute = (void*)(uint)DWMSBT.DWMSBT_DISABLE;

            RemoveDarkMode(handle);

            PInvoke.DwmSetWindowAttribute(new HWND(handle), (DWMWINDOWATTRIBUTE)DWMWA_MICA_EFFECT, pvAttribute,
                (uint)Marshal.SizeOf(typeof(int)));

            PInvoke.DwmSetWindowAttribute(new HWND(handle), (DWMWINDOWATTRIBUTE)DWMWA_SYSTEMBACKDROP_TYPE,
                backdropPvAttribute,
                (uint)Marshal.SizeOf(typeof(int)));
        }

        /// <summary>
        /// Tries to inform the operating system that this window uses dark mode.
        /// </summary>
        /// <param name="window">Window to apply effect.</param>
        public static void ApplyDarkMode(this Window window)
        {
            var windowHandle = new WindowInteropHelper(window).EnsureHandle();

            if (windowHandle == IntPtr.Zero) return;

            ApplyDarkMode(windowHandle);
        }

        /// <summary>
        /// Tries to inform the operating system that this <c>hWnd</c> uses dark mode.
        /// </summary>
        /// <param name="handle">Pointer to the window handle.</param>
        public static unsafe void ApplyDarkMode(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return;

            void* pvAttribute = (void*)(int)PvAttribute.Enable;
            var dwAttribute = DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;

            if (OSVersionHelper.OSVersion < new Version(10, 0, 18985))
            {
                dwAttribute = (DWMWINDOWATTRIBUTE)DWMWA_USE_IMMERSIVE_DARK_MODE_OLD;
            }

            PInvoke.DwmSetWindowAttribute(new HWND(handle), dwAttribute,
                pvAttribute,
                (uint)Marshal.SizeOf(typeof(int)));
        }

        /// <summary>
        /// Tries to clear the dark theme usage information.
        /// </summary>
        /// <param name="window">Window to remove effect.</param>
        public static void RemoveDarkMode(this Window window)
        {
            var windowHandle = new WindowInteropHelper(window).EnsureHandle();

            if (windowHandle == IntPtr.Zero) return;

            RemoveDarkMode(windowHandle);
        }

        /// <summary>
        /// Tries to clear the dark theme usage information.
        /// </summary>
        /// <param name="handle">Pointer to the window handle.</param>
        public static unsafe void RemoveDarkMode(IntPtr handle)
        {
            if (handle == IntPtr.Zero) { return; }

            var pvAttribute = (void*)(int)PvAttribute.Disable;
            var dwAttribute = DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;

            if (OSVersionHelper.OSVersion < new Version(10, 0, 18985))
            {
                dwAttribute = (DWMWINDOWATTRIBUTE)DWMWA_USE_IMMERSIVE_DARK_MODE_OLD;
            }

            PInvoke.DwmSetWindowAttribute(new HWND(handle), dwAttribute,
                pvAttribute,
                (uint)Marshal.SizeOf(typeof(int)));
        }

        /// <summary>
        /// Tries to remove default TitleBar from <c>hWnd</c>.
        /// </summary>
        /// <param name="window">Window to remove effect.</param>
        public static void RemoveTitleBar(this Window window)
        {
            var windowHandle = new WindowInteropHelper(window).EnsureHandle();

            if (windowHandle == IntPtr.Zero) return;

            RemoveTitleBar(windowHandle);
        }

        /// <summary>
        /// Tries to remove default TitleBar from <c>hWnd</c>.
        /// </summary>
        /// <param name="handle">Pointer to the window handle.</param>
        /// <returns><see langowrd="false"/> is problem occurs.</returns>
        private static bool RemoveTitleBar(IntPtr handle)
        {
            // Hide default TitleBar
            // https://stackoverflow.com/questions/743906/how-to-hide-close-button-in-wpf-window
            try
            {
                PInvoke.SetWindowLong(new HWND(handle), WINDOW_LONG_PTR_INDEX.GWL_STYLE, PInvoke.GetWindowLong(new HWND(handle), WINDOW_LONG_PTR_INDEX.GWL_STYLE) & ~0x80000);

                return true;
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e);
#endif
                return false;
            }
        }

        private static unsafe bool TryApplyNone(IntPtr handle)
        {
            if (OSVersionHelper.OSVersion >= new Version(10, 0, 22523))
            {
                void* backdropPvAttribute = (void*)(uint)DWMSBT.DWMSBT_AUTO;

                PInvoke.DwmSetWindowAttribute(new HWND(handle), (DWMWINDOWATTRIBUTE)DWMWA_SYSTEMBACKDROP_TYPE,
                    backdropPvAttribute,
                    (uint)Marshal.SizeOf(typeof(int)));

                return true;
            }
            else
            {
                Remove(handle);
                return true;
            }
        }

        private static unsafe bool TryApplyTabbed(IntPtr handle)
        {
            void* backdropPvAttribute = (void*)(uint)DWMSBT.DWMSBT_TABBEDWINDOW;

            PInvoke.DwmSetWindowAttribute(new HWND(handle), (DWMWINDOWATTRIBUTE)DWMWA_SYSTEMBACKDROP_TYPE,
                backdropPvAttribute,
                (uint)Marshal.SizeOf(typeof(int)));

            return true;
        }

        private static unsafe bool TryApplyMica(IntPtr handle)
        {
            void* backdropPvAttribute;

            if (OSVersionHelper.OSVersion>= new Version(10,0,22523))
            {
                backdropPvAttribute = (void*)(uint)DWMSBT.DWMSBT_MAINWINDOW;

                PInvoke.DwmSetWindowAttribute(new HWND(handle), (DWMWINDOWATTRIBUTE)DWMWA_SYSTEMBACKDROP_TYPE,
                    backdropPvAttribute,
                    (uint)Marshal.SizeOf(typeof(int)));

                return true;
            }

            if (!RemoveTitleBar(handle)) { return false; }

            backdropPvAttribute = (void*)(int)PvAttribute.Enable;

            PInvoke.DwmSetWindowAttribute(new HWND(handle), (DWMWINDOWATTRIBUTE)DWMWA_MICA_EFFECT,
                backdropPvAttribute,
                (uint)Marshal.SizeOf(typeof(int)));

            return true;
        }

        private static unsafe bool TryApplyAcrylic(IntPtr handle)
        {
            void* backdropPvAttribute = (void*)(uint)DWMSBT.DWMSBT_TRANSIENTWINDOW;

            PInvoke.DwmSetWindowAttribute(new HWND(handle), (DWMWINDOWATTRIBUTE)DWMWA_SYSTEMBACKDROP_TYPE,
                backdropPvAttribute,
                (uint)Marshal.SizeOf(typeof(int)));

            return true;
        }

        /// <summary>
        /// Allows a window to either use the accent color, or dark, according to the user Color Mode preferences.
        /// </summary>
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

        /// <summary>
        /// Allows to enter a value from 0 to 4 deciding on the imposed backdrop effect.
        /// </summary>
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        /// <summary>
        /// Indicates whether the window should use the Mica effect.
        /// <para>Windows 11 and above.</para>
        /// </summary>
        private const int DWMWA_MICA_EFFECT = 1029;

        /// <summary>
        /// Abstraction of pointer to an object containing the attribute value to set. The type of the value set depends on the value of the dwAttribute parameter.
        /// The DWMWINDOWATTRIBUTE enumeration topic indicates, in the row for each flag, what type of value you should pass a pointer to in the pvAttribute parameter.
        /// </summary>
        private enum PvAttribute
        {
            /// <summary>
            /// Object containing the <see langowrd="false"/> attribute value to set in dwmapi.h. 
            /// </summary>
            Disable = 0x00,
            /// <summary>
            /// Object containing the <see langowrd="true"/> attribute value to set in dwmapi.h. 
            /// </summary>
            Enable = 0x01
        }

        /// <summary>
        /// Collection of backdrop types.
        /// </summary>
        [Flags]
        private enum DWMSBT : uint
        {
            /// <summary>
            /// Automatically selects backdrop effect.
            /// </summary>
            DWMSBT_AUTO = 0,
            /// <summary>
            /// Turns off the backdrop effect.
            /// </summary>
            DWMSBT_DISABLE = 1,
            /// <summary>
            /// Sets Mica effect with generated wallpaper tint.
            /// </summary>
            DWMSBT_MAINWINDOW = 2,
            /// <summary>
            /// Sets acrlic effect.
            /// </summary>
            DWMSBT_TRANSIENTWINDOW = 3,
            /// <summary>
            /// Sets blurred wallpaper effect, like Mica without tint.
            /// </summary>
            DWMSBT_TABBEDWINDOW = 4
        }
    }
}
