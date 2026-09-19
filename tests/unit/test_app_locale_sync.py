from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FILE = ROOT / "apps/web/src/components/app-locale-sync.tsx"

def test_app_locale_sync_uses_os_locale_and_does_not_control_terminal_locale():
    source = FILE.read_text(encoding="utf-8")
    assert "navigator.language" in source
    assert "cfip-pro:app-locale:v1" in source
    assert "cfip:app-locale" in source
    assert "terminal" not in source.lower()
