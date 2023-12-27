select count(1) 
	from dbo.Workers 
	where name = @workerName 
		and date >= dateadd(millisecond, -@statusTimeout, getdate())