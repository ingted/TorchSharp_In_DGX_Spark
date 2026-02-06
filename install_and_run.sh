#!/bin/bash
# 下載並安裝 .NET Runtime (ARM64)
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --runtime dotnet

# 設定環境變數
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$DOTNET_ROOT:$PATH
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# 確認 dotnet
$DOTNET_ROOT/dotnet --info

# 執行原有的 test.sh
bash test.sh
