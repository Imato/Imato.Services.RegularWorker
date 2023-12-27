select * 
	from workers 
	where 
		name = @name 
		and host = @host 
		and appName = @appName 
	limit 1;