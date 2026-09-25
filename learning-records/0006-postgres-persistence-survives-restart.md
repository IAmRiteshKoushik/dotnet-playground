# PostgreSQL persistence survives API restart

The learner verified that a Todo remains available after stopping and restarting the ASP.NET Core process. This demonstrates that PostgreSQL, rather than in-process state, is the source of truth for the Todo API.
