import XCTest

/// Takes the App Store screenshots with the live data from slipways.de. `scripts/screenshots.sh` runs it once per
/// device, fixes the status bar and passes the folder for the images as `TEST_RUNNER_SCREENSHOT_DIR`. The app starts
/// in each scene through `LaunchState`, so nothing has to be tapped on the map.
@MainActor
final class ScreenshotTests: XCTestCase {
    func test01SlipwaysEurope() {
        shoot("01-slipanlagen-europa", tab: "slipways")
    }

    func test02SlipwaysLakes() {
        // Mecklenburgische Seenplatte around the Müritz.
        shoot("02-slipanlagen-seenplatte", tab: "slipways", area: "53.15,12.2,53.75,13.2")
    }

    func test03Slipway() {
        // Am Jachthafen, Kelheim (Donau): facts, marina and the nearest gauge.
        shoot("03-slipanlage-kelheim", tab: "slipways", area: "48.9,11.95,48.96,12.03", open: "eac96a93-fd47-4a9a-8771-0005805b9655")
    }

    func test04Marinas() {
        // Istria and the Kvarner Gulf.
        shoot("04-marinas-kroatien", tab: "marinas", area: "44.4,13.3,45.6,14.9")
    }

    func test05Marina() {
        // ACI Marina Rovinj as a sheet over the map.
        shoot("05-marina-rovinj", tab: "marinas", area: "44.9,13.45,45.25,13.85", open: "b3333ff0-2a53-4486-8a31-99096353fc41")
    }

    func test06Waters() {
        // The Mosel highlighted between Trier and Koblenz.
        shoot("06-gewaesser-mosel", tab: "waters", area: "49.6,6.5,50.45,7.7", select: "aae1f1d4-f79f-4c3a-83d5-debffd711981")
    }

    func test07Water() {
        // The Mosel with its gauges and slipways.
        shoot("07-gewaesser-pegel", tab: "waters", area: "49.6,6.5,50.45,7.7", open: "aae1f1d4-f79f-4c3a-83d5-debffd711981")
    }

    private func shoot(_ name: String, tab: String, area: String? = nil, open: String? = nil, select: String? = nil) {
        continueAfterFailure = false
        let app = XCUIApplication()
        app.launchArguments += ["-AppleLanguages", "(de)", "-AppleLocale", "de_DE"]
        app.launchEnvironment["SLIPWAYS_TAB"] = tab
        if let area { app.launchEnvironment["SLIPWAYS_AREA"] = area }
        if let open { app.launchEnvironment["SLIPWAYS_OPEN"] = open }
        if let select { app.launchEnvironment["SLIPWAYS_SELECT"] = select }
        app.launch()

        waitUntilLoaded(app)
        // Map tiles and the sheet animation have no element to wait for.
        Thread.sleep(forTimeInterval: 6)

        let screenshot = XCUIScreen.main.screenshot()
        let attachment = XCTAttachment(screenshot: screenshot)
        attachment.name = name
        attachment.lifetime = .keepAlways
        add(attachment)

        if let folder = ProcessInfo.processInfo.environment["SCREENSHOT_DIR"] {
            let url = URL(fileURLWithPath: folder).appendingPathComponent("\(name).png")
            do {
                try FileManager.default.createDirectory(atPath: folder, withIntermediateDirectories: true)
                try screenshot.pngRepresentation.write(to: url)
            } catch {
                XCTFail("Could not write \(url.path): \(error)")
            }
        }
        app.terminate()
    }

    /// Waits until the lists and maps have loaded: no "… werden geladen" and no spinner; fails when nothing came.
    private func waitUntilLoaded(_ app: XCUIApplication) {
        let loading = app.staticTexts.matching(NSPredicate(format: "label CONTAINS 'werden geladen'"))
        let failed = app.staticTexts.matching(NSPredicate(format: "label CONTAINS 'konnten nicht geladen werden'"))
        let deadline = Date().addingTimeInterval(90)
        // Give the first request a moment to start, so the check does not pass before anything shows.
        Thread.sleep(forTimeInterval: 2)
        while Date() < deadline {
            if failed.count > 0 {
                XCTFail("Loading from slipways.de failed")
                return
            }
            if loading.count == 0 && app.activityIndicators.count == 0 { return }
            Thread.sleep(forTimeInterval: 1)
        }
        // A spinner of MapKit's own may stay; only the app's own loading notice counts as a failure.
        if loading.count > 0 { XCTFail("Still loading after 90 seconds") }
    }
}
