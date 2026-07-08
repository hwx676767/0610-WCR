using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class TransparentWindow : MonoBehaviour
{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

    #region WinAPI

    [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] static extern int    GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] static extern int    SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
    [DllImport("user32.dll")] static extern bool   SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] static extern bool   GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool   ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("user32.dll")] static extern bool   ReleaseCapture();
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool   UpdateLayeredWindow(
        IntPtr hwnd, IntPtr hdcDst,
        ref POINT pptDst, ref SIZE psize,
        IntPtr hdcSrc, ref POINT pptSrc,
        uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hDC);
    [DllImport("gdi32.dll")] static extern bool   DeleteDC(IntPtr hDC);
    [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(
        IntPtr hDC, ref BITMAPINFO pbmi,
        uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
    [DllImport("gdi32.dll")] static extern bool   DeleteObject(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)] struct POINT { public int x, y; }
    [StructLayout(LayoutKind.Sequential)] struct SIZE  { public int cx, cy; }
    [StructLayout(LayoutKind.Sequential)] struct RECT  { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFOHEADER
    {
        public uint   biSize;
        public int    biWidth;
        public int    biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint   biCompression;
        public uint   biSizeImage;
        public int    biXPelsPerMeter;
        public int    biYPelsPerMeter;
        public uint   biClrUsed;
        public uint   biClrImportant;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public uint             bmiColors;
    }

    const int  GWL_EXSTYLE       = -20;
    const uint WS_EX_LAYERED     = 0x00080000;
    const uint WS_EX_TRANSPARENT = 0x00000020;
    const uint SWP_FRAMECHANGED  = 0x0020;
    const uint SWP_NOZORDER      = 0x0004;
    const uint ULW_ALPHA         = 0x00000002;
    const uint WM_NCLBUTTONDOWN  = 0xA1;
    const uint HTCAPTION         = 2;

    // 目标分辨率，按需修改
    const int TARGET_W = 1920;
    const int TARGET_H = 1080;

    #endregion

    IntPtr    _hwnd, _screenDC, _memDC, _hBitmap, _ppvBits;
    int       _lastW, _lastH;
    uint      _baseExStyle;
    bool      _mouseOnSprite;

    RenderTexture _rt;
    Texture2D     _readback;

    // -------------------------------------------------------------------------
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

void Start()
{
    _hwnd        = GetActiveWindow();
    _baseExStyle = (uint)GetWindowLong(_hwnd, GWL_EXSTYLE);

    SetWindowLong(_hwnd, GWL_EXSTYLE,
        _baseExStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);

    // 置顶 + 居中 + 设置尺寸
    int sysW = Display.main.systemWidth;
    int sysH = Display.main.systemHeight;
    int posX = (sysW - TARGET_W) / 2;
    int posY = (sysH - TARGET_H) / 2;
    SetWindowPos(_hwnd, HWND_TOPMOST, posX, posY, TARGET_W, TARGET_H,
        SWP_FRAMECHANGED);  // 不带 SWP_NOZORDER

    Camera.main.clearFlags      = CameraClearFlags.SolidColor;
    Camera.main.backgroundColor = new Color(0, 0, 0, 0);

    _screenDC = GetDC(IntPtr.Zero);
    RebuildBuffers(TARGET_W, TARGET_H);
}

    // -------------------------------------------------------------------------
    void RebuildBuffers(int w, int h)
    {
        // 释放旧资源
        if (_hBitmap != IntPtr.Zero) { DeleteObject(_hBitmap); _hBitmap = IntPtr.Zero; }
        if (_memDC   != IntPtr.Zero) { DeleteDC(_memDC);       _memDC   = IntPtr.Zero; }
        if (_rt      != null)        { _rt.Release();           Destroy(_rt); _rt = null; }
        if (_readback!= null)        { Destroy(_readback);      _readback = null; }

        _lastW = w;
        _lastH = h;

        // RenderTexture（含 alpha）
        _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        _rt.Create();
        Camera.main.targetTexture = _rt;

        // CPU 回读缓冲
        _readback = new Texture2D(w, h, TextureFormat.BGRA32, false);

        // DIB Section（BGRA，top-down）
        var info = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize        = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth       = w,
                biHeight      = -h,   // 负值 = top-down
                biPlanes      = 1,
                biBitCount    = 32,
                biCompression = 0
            }
        };
        _memDC   = CreateCompatibleDC(_screenDC);
        _hBitmap = CreateDIBSection(_memDC, ref info, 0, out _ppvBits, IntPtr.Zero, 0);
        SelectObject(_memDC, _hBitmap);
    }

    // -------------------------------------------------------------------------
    void Update()
    {
        // 鼠标在角色上且按下左键 → 系统原生拖拽
        if (_mouseOnSprite && Input.GetMouseButtonDown(0))
        {
            ReleaseCapture();
            SendMessage(_hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }
    }

    // -------------------------------------------------------------------------
    void LateUpdate()
    {
        // 用 GetWindowRect 获取真实像素尺寸，避免 Screen.width 漂移
        GetWindowRect(_hwnd, out RECT rect);
        int w = rect.Right  - rect.Left;
        int h = rect.Bottom - rect.Top;

        // 异常值保护
        if (w < 100 || h < 100) return;

        if (w != _lastW || h != _lastH)
            RebuildBuffers(w, h);

        // 1. 回读渲染结果
        var prev = RenderTexture.active;
        RenderTexture.active = _rt;
        _readback.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
        _readback.Apply();
        RenderTexture.active = prev;

        // 2. 垂直翻转 + 预乘 Alpha
        byte[] pixels  = _readback.GetRawTextureData();
        byte[] flipped = new byte[pixels.Length];
        int stride = w * 4;
        for (int row = 0; row < h; row++)
        {
            int srcRow = (h - 1 - row) * stride;
            int dstRow = row * stride;
            for (int col = 0; col < stride; col += 4)
            {
                byte b = pixels[srcRow + col];
                byte g = pixels[srcRow + col + 1];
                byte r = pixels[srcRow + col + 2];
                byte a = pixels[srcRow + col + 3];
                flipped[dstRow + col]     = (byte)(b * a / 255);
                flipped[dstRow + col + 1] = (byte)(g * a / 255);
                flipped[dstRow + col + 2] = (byte)(r * a / 255);
                flipped[dstRow + col + 3] = a;
            }
        }

        // 3. 写入 DIB
        Marshal.Copy(flipped, 0, _ppvBits, flipped.Length);

        // 4. UpdateLayeredWindow（用真实窗口位置，修复拖拽后复位）
        var pptDst = new POINT { x = rect.Left, y = rect.Top };
        var psize  = new SIZE  { cx = w,         cy = h };
        var pptSrc = new POINT { x = 0,          y = 0 };
        var blend  = new BLENDFUNCTION
        {
            BlendOp             = 0,
            BlendFlags          = 0,
            SourceConstantAlpha = 255,
            AlphaFormat         = 1
        };
        UpdateLayeredWindow(_hwnd, _screenDC,
            ref pptDst, ref psize,
            _memDC, ref pptSrc,
            0, ref blend, ULW_ALPHA);

        // 5. 动态穿透检测
        UpdateMouseTransparency();
    }

    // -------------------------------------------------------------------------
    void UpdateMouseTransparency()
    {
        int mx = (int)Input.mousePosition.x;
        int my = (int)Input.mousePosition.y;

        if (mx < 0 || mx >= _lastW || my < 0 || my >= _lastH)
        {
            SetPassThrough(true);
            return;
        }

        bool onSprite = _readback.GetPixel(mx, my).a > 0.01f;
        if (onSprite == _mouseOnSprite) return;

        _mouseOnSprite = onSprite;
        SetPassThrough(!onSprite);
    }

    void SetPassThrough(bool passThrough)
    {
        uint style = passThrough
            ? _baseExStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT
            : _baseExStyle | WS_EX_LAYERED;
        SetWindowLong(_hwnd, GWL_EXSTYLE, style);
    }

    // -------------------------------------------------------------------------
    void OnDestroy()
    {
        if (_hBitmap  != IntPtr.Zero) DeleteObject(_hBitmap);
        if (_memDC    != IntPtr.Zero) DeleteDC(_memDC);
        if (_screenDC != IntPtr.Zero) ReleaseDC(IntPtr.Zero, _screenDC);
    }

#endif
}