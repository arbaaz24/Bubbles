import pyautogui
import pygetwindow as gw
import time
import math  # Add this line

# Get the currently active window
active_window = gw.getActiveWindow()

if active_window:
    # Calculate center coordinates
    center_x = active_window.left + active_window.width // 2
    center_y = active_window.top + active_window.height // 2

    # Optional: move cursor to center
    #pyautogui.moveTo(center_x, center_y, duration=0.2)
    time.sleep(5)

    x, y = pyautogui.position()
    pyautogui.click(x, y) # this is a single click, to select it, we have to do 2 clicks

    # Simulate click
    #pyautogui.click()

    #the lines below are to test multiple clicks
    # pyautogui.click()
    # pyautogui.click()
    # pyautogui.click()
else:
    print("No active window detected.")

#this is rtandoen
#passerfefetre dfd aef  fdeed


# Function to click multiple points in a circular pattern
def click_circle(radius, center_x, center_y, num_clicks):
    for i in range(num_clicks):
        angle = 2 * math.pi * i / num_clicks  # Calculate angle for each point
        x = center_x + radius * math.cos(angle)  # X coordinate on circle
        y = center_y + radius * math.sin(angle)  # Y coordinate on circle
        pyautogui.click(x, y)  # Perform click at calculated position
        time.sleep(1)  # Short delay between clicks

click_circle(100, 700, 500, 10)  # Click 10 points around a circle of radius 100 centered at