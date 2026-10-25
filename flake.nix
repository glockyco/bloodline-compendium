{
  description = "Development shell for The Bloodline Compendium";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable";

  outputs =
    { nixpkgs, ... }:
    let
      # Microsoft's SHA-512 hashes from the .NET 10.0 release metadata.
      # nixpkgs-unstable no longer supports x86_64-darwin, so Intel Macs install the SDK directly.
      sdks = {
        aarch64-darwin = {
          rid = "osx-arm64";
          sha512 = "5893c66b48ea3674c7da18501059d606e96f3672fabe4d5e41762e692c7c83df3ee0b2b9dc5a0786191da2dc413d7335158101c14c935ab217f31ac2529770f3";
        };
        aarch64-linux = {
          rid = "linux-arm64";
          sha512 = "0966aa2b70f4677b8319be9df447a78a0dcd6164836425b55a68fd714f4023feb87b4cac21b9a996b79472ad54487cd0adcd2a9dfd570e2b2a1d26423a2baa9b";
        };
        x86_64-linux = {
          rid = "linux-x64";
          sha512 = "b7b3d8461cf98b07d2552ce9199702fa3ad010ac2671a5f8cd5136f7534e50f6855ba7bbb02b11494563f1d04156276a31cda1103b900e5eecd0d9473dc7b9c5";
        };
      };
      sdkVersion = "10.0.403";
      forEachSystem = nixpkgs.lib.genAttrs (builtins.attrNames sdks);

      # nixpkgs ships SDK 10.0.401 with runtime 10.0.12, which CVE-2026-96231 affects.
      # Override its binary SDK with Microsoft's 10.0.403 tarball until nixpkgs catches up.
      dotnetSdk =
        pkgs: system:
        let
          binary = pkgs.dotnetCorePackages.sdk_10_0_4xx-bin;
          unwrapped = binary.unwrapped.overrideAttrs {
            version = sdkVersion;
            src = pkgs.fetchurl {
              url = "https://builds.dotnet.microsoft.com/dotnet/Sdk/${sdkVersion}/dotnet-sdk-${sdkVersion}-${sdks.${system}.rid}.tar.gz";
              inherit (sdks.${system}) sha512;
            };
          };
        in
        binary.overrideAttrs {
          version = sdkVersion;
          src = unwrapped;
        };
    in
    {
      devShells = forEachSystem (
        system:
        let
          pkgs = nixpkgs.legacyPackages.${system};
        in
        {
          default = pkgs.mkShell {
            packages = [
              (dotnetSdk pkgs system)
              pkgs.bun
              pkgs.nodejs_22
            ];
            DOTNET_CLI_TELEMETRY_OPTOUT = "1";
            DOTNET_NOLOGO = "1";
          };
        }
      );
    };
}
