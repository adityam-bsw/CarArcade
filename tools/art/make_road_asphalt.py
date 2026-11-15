#!/usr/bin/env python3
"""Compatibility wrapper: asphalt strip only. The generator is make_road.py (parametrised by surface)."""
import os, sys
sys.argv += ["--surface", "asphalt"] if "--surface" not in sys.argv else []
exec(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "make_road.py")).read())
