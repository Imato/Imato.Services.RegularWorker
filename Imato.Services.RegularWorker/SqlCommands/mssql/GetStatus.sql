select top 1 * 
	from dbo.Workers 
	where name = @name 
		and host = @host 
		and appName = @appName