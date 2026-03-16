select count(*) from observation
where (@access AND (@where))
