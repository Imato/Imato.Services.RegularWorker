select * 
	from workers 
	where 
		name = @name 
		and host = @host 
		and app = @app
	limit 1;