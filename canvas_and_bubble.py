import tkinter as tk
import math
from PIL import Image, ImageTk
import pyautogui

# Take screenshot
screenshot = pyautogui.screenshot()
screenshot.save("screenshot.png")

points = []
drawing = True
threshold = 15  # pixels

def on_mouse_drag(event):
    global drawing
    if not drawing:
        return
    x, y = event.x, event.y
    if points:
        prev_x, prev_y = points[-1]
        canvas.create_line(prev_x, prev_y, x, y, fill='light blue', width=2)
    points.append((x, y))
    # Check if close to starting point and enough points drawn
    if len(points) > threshold:
        start_x, start_y = points[0]
        dist = math.hypot(x - start_x, y - start_y)
        if dist < threshold:
            drawing = False
            print("Shape closed! Points:", points)
            root.destroy()  # Close the window

# Tkinter window to display screenshot and draw
root = tk.Tk()
img = Image.open("screenshot.png")
tk_img = ImageTk.PhotoImage(img)
canvas = tk.Canvas(root, width=img.width, height=img.height)
canvas.pack()
canvas.create_image(0, 0, anchor="nw", image=tk_img)
canvas.bind('<B1-Motion>', on_mouse_drag)
root.mainloop()


