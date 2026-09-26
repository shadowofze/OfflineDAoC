# Offline DAoC v0.32 developer start

This is the normal Darkness Falls Beta source. It has no Sluaghbinder class.
For a clean, optional-class source tree, use the separate `v0.32b` Git tag.

Read [AGENTS.md](AGENTS.md) and [the LLM quickstart](docs/LLM-QUICKSTART.md)
before editing. [Development notes](docs/DEVELOPMENT.md) explain build inputs,
portable paths, world migrations, tests, and the source/playable split.

Darkness Falls ordinary bot goals are staged; its raids and hardest encounters
are not implemented for autonomous bots yet. Do not treat unit tests as proof
of a long live run. Work in a copy and keep player saves out of commits.
