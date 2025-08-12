import pythoncom
import win32con
import win32gui
import sys
import os
import struct
import ctypes
import win32com.server.util

CF_HDROP = 15
DROPEFFECT_COPY = 1

class FileDropDataObject:
    _com_interfaces_ = [pythoncom.IID_IDataObject]
    _public_methods_ = ['GetData', 'GetDataHere', 'QueryGetData', 'EnumFormatEtc']

    def __init__(self, file_paths):
        self.file_paths = file_paths

    def GetData(self, formatetc, medium):
        if formatetc.cfFormat == CF_HDROP:
            # Build DROPFILES structure
            offset = 20
            file_list = '\0'.join(self.file_paths) + '\0\0'
            file_list_bytes = file_list.encode('utf-16le')
            dropfiles_struct = struct.pack("<IiiII", offset, 0, 0, 0, 1)
            data = dropfiles_struct + file_list_bytes

            # Allocate global memory
            GMEM_MOVEABLE = 0x0002
            kernel32 = ctypes.windll.kernel32
            h_global = kernel32.GlobalAlloc(GMEM_MOVEABLE, len(data))
            p_global = kernel32.GlobalLock(h_global)
            ctypes.memmove(p_global, data, len(data))
            kernel32.GlobalUnlock(h_global)

            medium.set(pythoncom.TYMED_HGLOBAL, h_global)
            return 0  # S_OK
        return -2147221399  # DV_E_FORMATETC

    def GetDataHere(self, formatetc, medium):
        return -2147467263  # E_NOTIMPL

    def QueryGetData(self, formatetc):
        if formatetc.cfFormat == CF_HDROP:
            return 0  # S_OK
        return -2147221399  # DV_E_FORMATETC

    def EnumFormatEtc(self, direction):
        enum = win32gui.CreateDataObjectEnumFormatEtc([CF_HDROP])
        return enum

class DropSource:
    _com_interfaces_ = [pythoncom.IID_IDropSource]
    _public_methods_ = ['QueryContinueDrag', 'GiveFeedback']

    def QueryContinueDrag(self, escape, key_state):
        if escape:
            return win32con.DRAGDROP_S_CANCEL
        if not (key_state & win32con.MK_LBUTTON):
            return win32con.DRAGDROP_S_DROP
        return win32con.S_OK

    def GiveFeedback(self, effect):
        return win32con.DRAGDROP_S_USEDEFAULTCURSORS

def do_drag_drop(file_paths):
    data_obj = win32com.server.util.wrap(FileDropDataObject(file_paths), pythoncom.IID_IDataObject)
    drop_source = win32com.server.util.wrap(DropSource(), pythoncom.IID_IDropSource)
    effect = pythoncom.DoDragDrop(data_obj, drop_source, DROPEFFECT_COPY)
    print(f"Drag-drop completed with effect: {effect}")

if __name__ == "__main__":
    pythoncom.CoInitialize()  # <-- Call here, in the main thread
    try:
        if len(sys.argv) < 2:
            print("Usage: python drag_and_drop.py <file1> [file2 ...]")
            sys.exit(1)
        files = [os.path.abspath(f) for f in sys.argv[1:]]
        do_drag_drop(files)
    finally:
        pythoncom.CoUninitialize()
