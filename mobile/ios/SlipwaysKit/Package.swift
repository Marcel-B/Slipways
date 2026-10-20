// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "SlipwaysKit",
    platforms: [.iOS(.v17), .macOS(.v14)],
    products: [
        .library(name: "SlipwaysKit", targets: ["SlipwaysKit"]),
    ],
    targets: [
        .target(name: "SlipwaysKit"),
        .testTarget(name: "SlipwaysKitTests", dependencies: ["SlipwaysKit"]),
    ]
)
