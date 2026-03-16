select count(*) from reference
where (@access AND (@where))
