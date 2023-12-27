select count(1) 
	from workers 
	where name = @workerName 
		and host != @host 
		and date >= now() - (@statusTimeout / 1000 * interval'1 second');