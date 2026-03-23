cask "ttok" do
  name "ttok"
  desc "The fastest CLI tool for counting, encoding, decoding, and exploring BPE tokens"
  homepage "https://github.com/tryAGI/Tiktoken"

  arch arm: "osx-arm64", intel: "osx-x64"

  url "https://github.com/tryAGI/Tiktoken/releases/latest/download/ttok-#{arch}.tar.gz"
  sha256 :no_check

  binary "ttok"

  livecheck do
    url :homepage
    strategy :github_latest
  end
end
