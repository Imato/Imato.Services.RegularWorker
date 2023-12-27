select count(1) 
	from dbo.Workers
	where name = @workerName 
		and host != @host and date >= dateadd(millisecond, -@statusTimeout, getdate()) 
		and active = 1