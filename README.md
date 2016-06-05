# PyKiller

This Windows application automatically kills `python.exe` and `py.exe` when physical memory is starving.

I created this application because when physical memory is used-up, you can actually do nothing, including killing any process, simply because the whole OS is not responsive. In the meanwhile, there seems no easy replacement for Linux Out-of-Memory (OOM) killer on Windows, so I just need this simple daemon (or killer, more exactly) application.

This application requires .NET Framework 4.5 on Windows.

## Usage

Run this application first, then Python. After that, grab a cup of coffee.

You can configure this application by editing `PyKiller.exe.config` .

