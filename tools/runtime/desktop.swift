// Drives the macOS desktop for lab checks in `launch window`, which need what only the real cursor does, such as
// hovering. Run it with `swift tools/runtime/desktop.swift <command>` outside `nix develop`, whose toolchain lacks
// Swift. Raising and moving the mouse need the terminal to hold the Accessibility permission.
//   windows <pid>  lists the process's on-screen windows: id, bounds in points and title
//   raise <pid>    brings the process to the front and raises its windows
//   move <x> <y>   moves the mouse to a screen point in points, posting the mouse-moved events a hand would
import AppKit
import ApplicationServices
import CoreGraphics

func fail(_ message: String) -> Never {
    FileHandle.standardError.write((message + "\n").data(using: .utf8)!)
    exit(2)
}

let arguments = Array(CommandLine.arguments.dropFirst())
switch (arguments.first, arguments.count) {
case ("windows", 2):
    guard let pid = Int32(arguments[1]) else { fail("windows needs a process id") }
    let windows = CGWindowListCopyWindowInfo([.optionOnScreenOnly], kCGNullWindowID) as? [[String: Any]] ?? []
    for window in windows where (window[kCGWindowOwnerPID as String] as? Int32) == pid {
        let bounds = window[kCGWindowBounds as String] as? [String: Any] ?? [:]
        let parts = ["X", "Y", "Width", "Height"].map { "\(bounds[$0] ?? 0)" }.joined(separator: ",")
        print("\(window[kCGWindowNumber as String] ?? 0) \(parts) \(window[kCGWindowName as String] ?? "")")
    }
case ("raise", 2):
    guard let pid = Int32(arguments[1]) else { fail("raise needs a process id") }
    guard AXIsProcessTrusted() else { fail("The terminal lacks the Accessibility permission.") }
    // Activation by NSRunningApplication is only a request that the front app may refuse, while the accessibility
    // API's frontmost attribute takes effect.
    let application = AXUIElementCreateApplication(pid)
    AXUIElementSetAttributeValue(application, kAXFrontmostAttribute as CFString, kCFBooleanTrue)
    var value: CFTypeRef?
    if AXUIElementCopyAttributeValue(application, kAXWindowsAttribute as CFString, &value) == .success,
        let windows = value as? [AXUIElement]
    {
        for window in windows { AXUIElementPerformAction(window, kAXRaiseAction as CFString) }
    }
    usleep(700_000)
    print(NSWorkspace.shared.frontmostApplication?.processIdentifier == pid ? "front" : "not front")
case ("move", 3):
    guard let x = Double(arguments[1]), let y = Double(arguments[2]) else { fail("move needs x and y") }
    let start = CGEvent(source: nil)?.location ?? CGPoint(x: x, y: y)
    let steps = 12
    for step in 1...steps {
        let t = Double(step) / Double(steps)
        let point = CGPoint(x: start.x + (x - start.x) * t, y: start.y + (y - start.y) * t)
        CGEvent(mouseEventSource: nil, mouseType: .mouseMoved, mouseCursorPosition: point, mouseButton: .left)?
            .post(tap: .cghidEventTap)
        usleep(15_000)
    }
    // A last small move inside the target, as a hand comes to rest. Run hover-check raised tooltips this way.
    for dx in [1.0, 0.0] {
        CGEvent(mouseEventSource: nil, mouseType: .mouseMoved, mouseCursorPosition: CGPoint(x: x + dx, y: y), mouseButton: .left)?
            .post(tap: .cghidEventTap)
        usleep(40_000)
    }
    print("\(CGEvent(source: nil)?.location ?? .zero)")
default:
    fail("usage: desktop.swift windows <pid> | raise <pid> | move <x> <y>")
}
