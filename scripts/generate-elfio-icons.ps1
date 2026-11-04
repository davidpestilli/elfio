# Regenera ícones do Studio (fundo transparente) a partir de Assets/elf_domestic.webp
$ErrorActionPreference = "Stop"
$script = Join-Path $PSScriptRoot "generate-elfio-icons.py"
python $script
